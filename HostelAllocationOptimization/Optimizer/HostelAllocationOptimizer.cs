using Gurobi;
using HostelAllocationOptimization.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HostelAllocationOptimization.Optimizer
{
    public static class HostelAllocationOptimizer
    {
        public static List<DailyRoomAllocation> Optimize(HostelAllocationDTO hostelAllocation)
        {
            try
            {
                int numDays = hostelAllocation.NumDays;
                int numRooms = hostelAllocation.NumRooms;
                int numGroups = hostelAllocation.GroupsSizes.Count();
                int[] groupSize = hostelAllocation.GroupsSizes;
                int[] roomCapacity = hostelAllocation.RoomCapacity;

                List<Tuple<int, int>> groupsDemands = new List<Tuple<int, int>>(); // groupsDemands(group, day)
                groupsDemands = hostelAllocation.GroupsDemands;         
                // Group 0
                //groupsDemands.Add(new Tuple<int, int>(0, 0));
                //groupsDemands.Add(new Tuple<int, int>(0, 1));
                //groupsDemands.Add(new Tuple<int, int>(0, 2));
                //// Group 1
                //groupsDemands.Add(new Tuple<int, int>(1, 0));
                //groupsDemands.Add(new Tuple<int, int>(1, 1));
                //// Group 2
                //groupsDemands.Add(new Tuple<int, int>(2, 0));
                //groupsDemands.Add(new Tuple<int, int>(2, 1));
                //groupsDemands.Add(new Tuple<int, int>(2, 2));
                //// Group 3
                //groupsDemands.Add(new Tuple<int, int>(3, 1));
                //groupsDemands.Add(new Tuple<int, int>(3, 2));
                //// Group 4
                //groupsDemands.Add(new Tuple<int, int>(4, 1));
                //groupsDemands.Add(new Tuple<int, int>(4, 2));
                //// Group 5
                //groupsDemands.Add(new Tuple<int, int>(5, 1));
                //groupsDemands.Add(new Tuple<int, int>(5, 2));
                //// Group 6
                //groupsDemands.Add(new Tuple<int, int>(6, 2));

                List<Tuple<int, int>> initialAllocation = new List<Tuple<int, int>>(); // initialAlocation<group, room>
                initialAllocation = hostelAllocation.InitialAllocation;
                //initialAllocation.Add(new Tuple<int, int>(0, 0));
                //initialAllocation.Add(new Tuple<int, int>(1, 1));
                //initialAllocation.Add(new Tuple<int, int>(2, 2));

                GRBEnv env = new GRBEnv("hostel.log");
                GRBModel model = new GRBModel(env);

                model.ModelName = "hostel";

                //variável de decisão: xijk - se o grupo i está no quyarto j no dia k
                GRBVar[,,] x = new GRBVar[numGroups, numRooms, numDays];
                for (int i = 0; i < numGroups; i++)
                {
                    for (int j = 0; j < numRooms; j++)
                    {
                        for (int k = 0; k < numDays; k++)
                        {
                            x[i, j, k] = model.AddVar(0, 1, 0, GRB.BINARY, "Grupo " + x + " quarto " + j + " dia " + k);
                        }
                    }
                }

                //Initialize Rooms with initial alocation
                foreach (var alocation in initialAllocation)
                {
                    x[alocation.Item1, alocation.Item2, 0].Set(GRB.DoubleAttr.LB, 1);
                }

                //variável de decisão: yik - se o grupo i mudou de quarto no dia k
                GRBVar[,] y = new GRBVar[numGroups, numDays];
                for (int i = 0; i < numGroups; i++)
                {
                    for (int k = 0; k < numDays; k++)
                    {
                        y[i, k] = model.AddVar(0, 1, 0, GRB.BINARY, "Grupo " + i + " mudou quarto no dia " + k);
                    }
                }

                //Restrição 1: capacidade do quarto
                for (int j = 0; j < numRooms; j++)
                {
                    for (int k = 0; k < numDays; k++)
                    {
                        GRBLinExpr roomAlocation = 0.0;
                        for (int i = 0; i < numGroups; i++)
                        {
                            roomAlocation.AddTerm(groupSize[i], x[i, j, k]);
                        }
                        model.AddConstr(roomAlocation <= roomCapacity[j], "Capacity room " + j);
                    }
                }

                //Restrição 2: todo grupo deverá estar alocado em algum quarto nos dias que demandou
                for (int i = 0; i < numGroups; i++)
                {
                    for (int k = 0; k < numDays; k++)
                    {
                        GRBLinExpr demGroup = 0.0;
                        if (groupsDemands.Any(d => d.Item1 == i && d.Item2 == k))
                        {
                            for (int j = 0; j < numRooms; j++)
                            {
                                demGroup.AddTerm(1, x[i, j, k]);
                            }
                            model.AddConstr(demGroup == 1, "Alocação grupo " + i + " no dia " + k);
                        }
                    }
                }

                //Restrição 3: se o grupo i tivesse no quarto j no dia k e no dia k + 1 ele não estiver, ele mudou de quarto
                for (int i = 0; i < numGroups; i++)
                {
                    for (int j = 0; j < numRooms; j++)
                    {
                        for (int k = 0; k < numDays - 1; k++)
                        {
                            if(k == 0 && initialAllocation.Any(d => d.Item1 == i) && groupsDemands.Any(d => d.Item1 == i && d.Item2 == k + 1))
                            {
                                model.AddConstr((1 - x[i, j, k]) + x[i, j, k + 1] + y[i, k] >= 1, "Grupo " + i + " mudou de quarto dia " + k);
                            } else if(groupsDemands.Any(d => d.Item1 == i && d.Item2 == k) && groupsDemands.Any(d => d.Item1 == i && d.Item2 == k + 1)) // O grupo tem demanda para o dia seguinte
                            {
                                model.AddConstr((1 - x[i, j, k]) + x[i, j, k + 1] + y[i, k] >= 1, "Grupo " + i + " mudou de quarto dia " + k);
                            }

                        }
                    }
                }

                model.ModelSense = GRB.MINIMIZE;

                GRBLinExpr obj = 0;
                for (int i = 0; i < numGroups; i++)
                {
                    for (int k = 0; k < numDays; k++)
                    {
                        obj.AddTerm(groupSize[i], y[i, k]);
                    }
                }
                model.SetObjective(obj, GRB.MINIMIZE);

                // Optimize model
                model.Optimize();

                if(model.Status == GRB.Status.INFEASIBLE)
                {
                    throw new Exception("Infeasible model");
                }

                var allocation = ListDailyGroupsAllocation(x, y, model.ObjVal);
                var roomsAllocation = ListDailyRoomAllocation(x, y, model.ObjVal);

                // Dispose of model and env
                model.Dispose();
                env.Dispose();

                return roomsAllocation;
            }

            catch (GRBException e)
            {
                throw;
                //Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
            }
        }

        public static List<DailyRoomAllocation> Optimize(string jsonFileName)
        {
            try
            {
                string path= "C:\\Users\\alanw\\OneDrive\\Documentos\\GitHub\\HostelAllocation\\HostelAllocationOptimization\\bin\\Debug\\netcoreapp2.0\\" + jsonFileName;
                using (StreamReader file = File.OpenText(path))
                {
                    var json = file.ReadToEnd();
                    var hostelAllocation = JsonConvert.DeserializeObject<HostelAllocationDTO>(json);

                    int numDays = hostelAllocation.NumDays;
                    int numRooms = hostelAllocation.NumRooms;
                    int numGroups = hostelAllocation.GroupsSizes.Count();
                    int[] groupSize = hostelAllocation.GroupsSizes;
                    int[] roomCapacity = hostelAllocation.RoomCapacity;

                    List<Tuple<int, int>> groupsDemands = new List<Tuple<int, int>>(); // groupsDemands(group, day)
                    groupsDemands = hostelAllocation.GroupsDemands;                                             // Group 0
                    //groupsDemands.Add(new Tuple<int, int>(0, 0));
                    //groupsDemands.Add(new Tuple<int, int>(0, 1));
                    //// Group 1
                    //groupsDemands.Add(new Tuple<int, int>(1, 0));
                    //groupsDemands.Add(new Tuple<int, int>(1, 1));
                    //// Group 2
                    //groupsDemands.Add(new Tuple<int, int>(2, 0));
                    //groupsDemands.Add(new Tuple<int, int>(2, 1));
                    //groupsDemands.Add(new Tuple<int, int>(2, 2));
                    //// Group 3
                    //groupsDemands.Add(new Tuple<int, int>(3, 1));
                    //groupsDemands.Add(new Tuple<int, int>(3, 2));
                    //// Group 4
                    //groupsDemands.Add(new Tuple<int, int>(4, 1));
                    //groupsDemands.Add(new Tuple<int, int>(4, 2));
                    //// Group 5
                    //groupsDemands.Add(new Tuple<int, int>(5, 1));
                    //groupsDemands.Add(new Tuple<int, int>(5, 2));
                    //// Group 6
                    //groupsDemands.Add(new Tuple<int, int>(6, 2));

                    List<Tuple<int, int>> initialAllocation = new List<Tuple<int, int>>(); // initialAlocation<group, room>
                    initialAllocation = hostelAllocation.InitialAllocation;
                    //initialAllocation.Add(new Tuple<int, int>(0, 0));
                    //initialAllocation.Add(new Tuple<int, int>(1, 1));
                    //initialAllocation.Add(new Tuple<int, int>(2, 2));

                    GRBEnv env = new GRBEnv("hostel.log");
                    GRBModel model = new GRBModel(env);

                    model.ModelName = "hostel";

                    //variável de decisão: xijk - se o grupo i está no quyarto j no dia k
                    GRBVar[,,] x = new GRBVar[numGroups, numRooms, numDays];
                    for (int i = 0; i < numGroups; i++)
                    {
                        for (int j = 0; j < numRooms; j++)
                        {
                            for (int k = 0; k < numDays; k++)
                            {
                                x[i, j, k] = model.AddVar(0, 1, 0, GRB.BINARY, "Grupo " + x + " quarto " + j + " dia " + k);
                            }
                        }
                    }

                    //Initialize Rooms with initial alocation
                    foreach (var alocation in initialAllocation)
                    {
                        x[alocation.Item1, alocation.Item2, 0].Set(GRB.DoubleAttr.LB, 1);
                    }

                    //variável de decisão: yik - se o grupo i mudou de quarto no dia k
                    GRBVar[,] y = new GRBVar[numGroups, numDays];
                    for (int i = 0; i < numGroups; i++)
                    {
                        for (int k = 0; k < numDays; k++)
                        {
                            y[i, k] = model.AddVar(0, 1, 0, GRB.BINARY, "Grupo " + i + " mudou quarto no dia " + k);
                        }
                    }

                    //Restrição 1: capacidade do quarto
                    for (int j = 0; j < numRooms; j++)
                    {
                        for (int k = 0; k < numDays; k++)
                        {
                            GRBLinExpr roomAlocation = 0.0;
                            for (int i = 0; i < numGroups; i++)
                            {
                                roomAlocation.AddTerm(groupSize[i], x[i, j, k]);
                            }
                            model.AddConstr(roomAlocation <= roomCapacity[j], "Capacity room " + j);
                        }
                    }

                    //Restrição 2: todo grupo deverá estar alocado em algum quarto nos dias que demandou
                    for (int i = 0; i < numGroups; i++)
                    {
                        for (int k = 0; k < numDays; k++)
                        {
                            GRBLinExpr demGroup = 0.0;
                            if (groupsDemands.Any(d => d.Item1 == i && d.Item2 == k))
                            {
                                for (int j = 0; j < numRooms; j++)
                                {
                                    demGroup.AddTerm(1, x[i, j, k]);
                                }
                                model.AddConstr(demGroup == 1, "Alocação grupo " + i + " no dia " + k);
                            }
                        }
                    }

                    //Restrição 3: se o grupo i tivesse no quarto j no dia k e no dia k + 1 ele não estiver, ele mudou de quarto
                    for (int i = 0; i < numGroups; i++)
                    {
                        for (int j = 0; j < numRooms; j++)
                        {
                            for (int k = 0; k < numDays - 1; k++)
                            {
                                if (k == 0 && initialAllocation.Any(d => d.Item1 == i) && groupsDemands.Any(d => d.Item1 == i && d.Item2 == k + 1))
                                {
                                    model.AddConstr((1 - x[i, j, k]) + x[i, j, k + 1] + y[i, k + 1] >= 1, "Grupo " + i + " mudou de quarto dia " + k);
                                }
                                else if (groupsDemands.Any(d => d.Item1 == i && d.Item2 == k) && groupsDemands.Any(d => d.Item1 == i && d.Item2 == k + 1)) // O grupo tem demanda para o dia seguinte
                                {
                                    model.AddConstr((1 - x[i, j, k]) + x[i, j, k + 1] + y[i, k + 1] >= 1, "Grupo " + i + " mudou de quarto dia " + k);
                                }

                            }
                        }
                    }

                    model.ModelSense = GRB.MINIMIZE;

                    GRBLinExpr obj = 0;
                    for (int i = 0; i < numGroups; i++)
                    {
                        for (int k = 0; k < numDays; k++)
                        {
                            obj.AddTerm(groupSize[i], y[i, k]);
                        }
                    }
                    model.SetObjective(obj, GRB.MINIMIZE);

                    // Optimize model
                    model.Optimize();

                    if (model.Status == GRB.Status.INFEASIBLE)
                    {
                        throw new ApplicationException("Infeasible model");
                    }

                    //var allocation = ListDailyGroupsAllocation(x, y, model.ObjVal);
                    var roomsAllocation = ListDailyRoomAllocation(x, y, model.ObjVal);

                    // Dispose of model and env
                    model.Dispose();
                    env.Dispose();

                    return roomsAllocation;
                }
            }

            catch (GRBException e)
            {
                throw e;
            }
        }

        public static List<DailyRoomAllocation> OptimizeWithGroupSplit(string jsonFileName)
        {
            try
            {
                string path = "C:\\Users\\alanw\\OneDrive\\Documentos\\GitHub\\HostelAllocation\\HostelAllocationOptimization\\bin\\Debug\\netcoreapp2.0\\" + jsonFileName;
                using (StreamReader file = File.OpenText(path))
                {
                    var json = file.ReadToEnd();
                    var hostelAllocation = JsonConvert.DeserializeObject<HostelAllocationDTO>(json);

                    int numDays = hostelAllocation.NumDays;
                    int numRooms = hostelAllocation.NumRooms;
                    int numGroups = hostelAllocation.GroupsSizes.Count();
                    int[] groupSize = hostelAllocation.GroupsSizes;
                    int[] roomCapacity = hostelAllocation.RoomCapacity;

                    List<Tuple<int, int>> groupsDemands = new List<Tuple<int, int>>(); // groupsDemands(group, day)
                    groupsDemands = hostelAllocation.GroupsDemands;

                    List<Tuple<int, int>> initialAllocation = new List<Tuple<int, int>>(); // initialAlocation<group, room>
                    initialAllocation = hostelAllocation.InitialAllocation;

                    GRBEnv env = new GRBEnv("hostel.log");
                    GRBModel model = new GRBModel(env);

                    model.ModelName = "hostel";

                    int numPeople = groupSize.Sum();
                    Dictionary<int, int> personGroupRelation = AssociatePersonToGroup(numGroups, groupSize);

                    //variável de decisão: xijk - se a pessoa i está no quarto j no dia k
                    GRBVar[,,] x = new GRBVar[numPeople, numRooms, numDays];
                    for (int i = 0; i < numPeople; i++)
                    {
                        for (int j = 0; j < numRooms; j++)
                        {
                            for (int k = 0; k < numDays; k++)
                            {
                                x[i, j, k] = model.AddVar(0, 1, 0, GRB.BINARY, "Grupo " + x + " quarto " + j + " dia " + k);
                            }
                        }
                    }

                    //Initialize Rooms with initial alocation
                    foreach (var alocation in initialAllocation)
                    {
                        var groupPeople = personGroupRelation.Where(p => p.Value == alocation.Item1).ToList();
                        foreach(var person in groupPeople)
                        {
                            x[person.Key, alocation.Item2, 0].Set(GRB.DoubleAttr.LB, 1);
                        }
                    }

                    //variável de decisão: yik - se o grupo i mudou de quarto no dia k
                    GRBVar[,] y = new GRBVar[numGroups, numDays];
                    for (int i = 0; i < numGroups; i++)
                    {
                        for (int k = 0; k < numDays; k++)
                        {
                            y[i, k] = model.AddVar(0, 1, 0, GRB.BINARY, "Grupo " + i + " mudou quarto no dia " + k);
                        }
                    }

                    //variável de decisão: Zik se o grupo i foi quebrado no dia k
                    GRBVar[,] z = new GRBVar[numGroups, numDays];
                    for (int i = 0; i < numGroups; i++)
                    {
                        for (int k = 0; k < numDays; k++)
                        {
                            z[i, k] = model.AddVar(0, 1, 0, GRB.BINARY, "Grupo " + i + " foi quebrado no dia " + k);
                        }
                    }

                    //Restrição 1: capacidade do quarto
                    for (int j = 0; j < numRooms; j++)
                    {
                        for (int k = 0; k < numDays; k++)
                        {
                            GRBLinExpr roomAlocation = 0.0;
                            for (int i = 0; i < numPeople; i++)
                            {
                                roomAlocation.AddTerm(1, x[i, j, k]);
                            }
                            model.AddConstr(roomAlocation <= roomCapacity[j], "Capacity room " + j);
                        }
                    }

                    //Restrição 2: todas as pessoas deverão estar alocadas em algum quarto nos dias que demandou
                    for (int i = 0; i < numPeople; i++)
                    {
                        for (int k = 0; k < numDays; k++)
                        {
                            GRBLinExpr demGroup = 0.0;
                            var personGroup = personGroupRelation[i];
                            var groupAlloc = groupsDemands.Any(d => d.Item1 == personGroup && d.Item2 == k);
                            if (groupAlloc)
                            {
                                for (int j = 0; j < numRooms; j++)
                                {
                                    demGroup.AddTerm(1, x[i, j, k]);
                                }
                                model.AddConstr(demGroup == 1, "Alocação pessoa " + i + " no dia " + k);
                            }
                        }
                    }

                    //Restrição 3: se o grupo i tivesse no quarto j no dia k e no dia k + 1 ele não estiver, ele mudou de quarto
                    for (int i = 0; i < numPeople; i++)
                    {
                        for (int j = 0; j < numRooms; j++)
                        {
                            for (int k = 0; k < numDays - 1; k++)
                            {
                                if (k == 0 && initialAllocation.Any(d => d.Item1 == personGroupRelation[i]) && groupsDemands.Any(d => d.Item1 == personGroupRelation[i] && d.Item2 == k + 1))
                                {
                                    model.AddConstr((1 - x[i, j, k]) + x[i, j, k + 1] + y[personGroupRelation[i], k + 1] >= 1, "Grupo " + i + " mudou de quarto dia " + k);
                                }
                                else if (groupsDemands.Any(d => d.Item1 == personGroupRelation[i] && d.Item2 == k) && groupsDemands.Any(d => d.Item1 == personGroupRelation[i] && d.Item2 == k + 1)) // O grupo tem demanda para o dia seguinte
                                {
                                    model.AddConstr((1 - x[i, j, k]) + x[i, j, k + 1] + y[personGroupRelation[i], k + 1] >= 1, "Grupo " + i + " mudou de quarto dia " + k);
                                }

                            }
                        }
                    }

                    // Restrição 4: quebra do grupo
                    for (int k = 0; k < numDays; k++)
                    {
                        for(int i = 0; i < numGroups; i++)
                        {
                            for(int j = 0; j < numRooms; j++)
                            {
                                GRBLinExpr totalPeopleGroupInRoom = 0.0;
                                var peopleFromGroup = personGroupRelation.Where(p => p.Value == i).ToList();
                                foreach(var person in peopleFromGroup)
                                {
                                    double coeff = (double)1 / (double)groupSize[i];
                                    totalPeopleGroupInRoom.AddTerm(coeff, x[person.Key, j, k]);
                                }
                                foreach (var person in peopleFromGroup)
                                {
                                    model.AddConstr(totalPeopleGroupInRoom + z[i, k] >= x[person.Key, j, k], "Grupo " + i + " está todo no mesmo quarto ou quebrou");
                                }
                            }
                        }
                    }

                    model.ModelSense = GRB.MINIMIZE;

                    GRBLinExpr obj = 0;
                    for (int i = 0; i < numGroups; i++)
                    {
                        for (int k = 0; k < numDays; k++)
                        {
                            obj.AddTerm(groupSize[i], y[i, k]);
                            obj.AddTerm(groupSize[i], z[i, k]);
                        }
                    }

                    model.SetObjective(obj, GRB.MINIMIZE);

                    // Optimize model
                    model.Optimize();


                    if (model.Status == GRB.Status.INFEASIBLE)
                    {
                        throw new Exception("Infeasible model");
                    }

                    var allocation = ListDailyRoomAllocation(x, y, z, personGroupRelation, model.ObjVal);
                    //var roomsAllocation = ListDailyPeopleRoomAllocation(x, z, model.ObjVal);

                    // Dispose of model and env
                    model.Dispose();
                    env.Dispose();

                    return allocation;
                }
            }

            catch (GRBException e)
            {
                //Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
                throw e;
            }
        }

        public static List<ModelResultDTO> OptimizeWithGroupSplit(List<string> fileNames)
        {
            List<ModelResultDTO> results = new List<ModelResultDTO>();
            foreach (var jsonFileName in fileNames)
            {
                try
                {
                    DateTime startExecution = DateTime.Now;
                    string path = "C:\\Users\\alanw\\OneDrive\\Documentos\\GitHub\\HostelAllocation\\HostelAllocationOptimization\\bin\\Debug\\netcoreapp2.0\\" + jsonFileName;
                    using (StreamReader file = File.OpenText(path))
                    {
                        var json = file.ReadToEnd();
                        var hostelAllocation = JsonConvert.DeserializeObject<HostelAllocationDTO>(json);

                        int numDays = hostelAllocation.NumDays;
                        int numRooms = hostelAllocation.NumRooms;
                        int numGroups = hostelAllocation.GroupsSizes.Count();
                        int[] groupSize = hostelAllocation.GroupsSizes;
                        int[] roomCapacity = hostelAllocation.RoomCapacity;

                        List<Tuple<int, int>> groupsDemands = new List<Tuple<int, int>>(); // groupsDemands(group, day)
                        groupsDemands = hostelAllocation.GroupsDemands;

                        List<Tuple<int, int>> initialAllocation = new List<Tuple<int, int>>(); // initialAlocation<group, room>
                        initialAllocation = hostelAllocation.InitialAllocation;

                        GRBEnv env = new GRBEnv();
                        GRBModel model = new GRBModel(env);

                        model.ModelName = "hostel";

                        int numPeople = groupSize.Sum();
                        Dictionary<int, int> personGroupRelation = AssociatePersonToGroup(numGroups, groupSize);

                        //variável de decisão: xijk - se a pessoa i está no quarto j no dia k
                        GRBVar[,,] x = new GRBVar[numPeople, numRooms, numDays];
                        for (int i = 0; i < numPeople; i++)
                        {
                            for (int j = 0; j < numRooms; j++)
                            {
                                for (int k = 0; k < numDays; k++)
                                {
                                    x[i, j, k] = model.AddVar(0, 1, 0, GRB.BINARY, "Grupo " + x + " quarto " + j + " dia " + k);
                                }
                            }
                        }

                        //Initialize Rooms with initial alocation
                        foreach (var alocation in initialAllocation)
                        {
                            var groupPeople = personGroupRelation.Where(p => p.Value == alocation.Item1).ToList();
                            foreach (var person in groupPeople)
                            {
                                x[person.Key, alocation.Item2, 0].Set(GRB.DoubleAttr.LB, 1);
                            }
                        }

                        //variável de decisão: yik - se o grupo i mudou de quarto no dia k
                        GRBVar[,] y = new GRBVar[numGroups, numDays];
                        for (int i = 0; i < numGroups; i++)
                        {
                            for (int k = 0; k < numDays; k++)
                            {
                                y[i, k] = model.AddVar(0, 1, 0, GRB.BINARY, "Grupo " + i + " mudou quarto no dia " + k);
                            }
                        }

                        //variável de decisão: Zik se o grupo i foi quebrado no dia k
                        GRBVar[,] z = new GRBVar[numGroups, numDays];
                        for (int i = 0; i < numGroups; i++)
                        {
                            for (int k = 0; k < numDays; k++)
                            {
                                z[i, k] = model.AddVar(0, 1, 0, GRB.BINARY, "Grupo " + i + " foi quebrado no dia " + k);
                            }
                        }

                        //Restrição 1: capacidade do quarto
                        for (int j = 0; j < numRooms; j++)
                        {
                            for (int k = 0; k < numDays; k++)
                            {
                                GRBLinExpr roomAlocation = 0.0;
                                for (int i = 0; i < numPeople; i++)
                                {
                                    roomAlocation.AddTerm(1, x[i, j, k]);
                                }
                                model.AddConstr(roomAlocation <= roomCapacity[j], "Capacity room " + j);
                            }
                        }

                        //Restrição 2: todas as pessoas deverão estar alocadas em algum quarto nos dias que demandou
                        for (int i = 0; i < numPeople; i++)
                        {
                            for (int k = 0; k < numDays; k++)
                            {
                                GRBLinExpr demGroup = 0.0;
                                var personGroup = personGroupRelation[i];
                                var groupAlloc = groupsDemands.Any(d => d.Item1 == personGroup && d.Item2 == k);
                                if (groupAlloc)
                                {
                                    for (int j = 0; j < numRooms; j++)
                                    {
                                        demGroup.AddTerm(1, x[i, j, k]);
                                    }
                                    model.AddConstr(demGroup == 1, "Alocação pessoa " + i + " no dia " + k);
                                }
                            }
                        }

                        //Restrição 3: se o grupo i tivesse no quarto j no dia k e no dia k + 1 ele não estiver, ele mudou de quarto
                        for (int i = 0; i < numPeople; i++)
                        {
                            for (int j = 0; j < numRooms; j++)
                            {
                                for (int k = 0; k < numDays - 1; k++)
                                {
                                    if (k == 0 && initialAllocation.Any(d => d.Item1 == personGroupRelation[i]) && groupsDemands.Any(d => d.Item1 == personGroupRelation[i] && d.Item2 == k + 1))
                                    {
                                        model.AddConstr((1 - x[i, j, k]) + x[i, j, k + 1] + y[personGroupRelation[i], k + 1] >= 1, "Grupo " + i + " mudou de quarto dia " + k);
                                    }
                                    else if (groupsDemands.Any(d => d.Item1 == personGroupRelation[i] && d.Item2 == k) && groupsDemands.Any(d => d.Item1 == personGroupRelation[i] && d.Item2 == k + 1)) // O grupo tem demanda para o dia seguinte
                                    {
                                        model.AddConstr((1 - x[i, j, k]) + x[i, j, k + 1] + y[personGroupRelation[i], k + 1] >= 1, "Grupo " + i + " mudou de quarto dia " + k);
                                    }

                                }
                            }
                        }

                        // Restrição 4: quebra do grupo
                        for (int k = 0; k < numDays; k++)
                        {
                            for (int i = 0; i < numGroups; i++)
                            {
                                for (int j = 0; j < numRooms; j++)
                                {
                                    GRBLinExpr totalPeopleGroupInRoom = 0.0;
                                    var peopleFromGroup = personGroupRelation.Where(p => p.Value == i).ToList();
                                    foreach (var person in peopleFromGroup)
                                    {
                                        double coeff = (double)1 / (double)groupSize[i];
                                        totalPeopleGroupInRoom.AddTerm(coeff, x[person.Key, j, k]);
                                    }
                                    foreach (var person in peopleFromGroup)
                                    {
                                        model.AddConstr(totalPeopleGroupInRoom + z[i, k] >= x[person.Key, j, k], "Grupo " + i + " está todo no mesmo quarto ou quebrou");
                                    }
                                }
                            }
                        }

                        model.ModelSense = GRB.MINIMIZE;

                        GRBLinExpr obj = 0;
                        for (int i = 0; i < numGroups; i++)
                        {
                            for (int k = 0; k < numDays; k++)
                            {
                                obj.AddTerm(groupSize[i], y[i, k]);
                                obj.AddTerm(groupSize[i], z[i, k]);
                            }
                        }

                        model.SetObjective(obj, GRB.MINIMIZE);

                        // Optimize model
                        model.Optimize();


                        if (model.Status == GRB.Status.INFEASIBLE)
                        {
                            throw new Exception("Infeasible model");
                        }

                        var allocation = ListDailyRoomAllocation(x, y, z, personGroupRelation, model.ObjVal);
                        //var roomsAllocation = ListDailyPeopleRoomAllocation(x, z, model.ObjVal);
                        DateTime endExecution = DateTime.Now;
                        results.Add(new ModelResultDTO(jsonFileName, allocation, startExecution - endExecution, model.NumVars, model.NumConstrs));

                        // Dispose of model and env
                        model.Dispose();
                        env.Dispose();
                    }
                }

                catch (GRBException e)
                {
                    //Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
                    throw e;
                }
            }

            using (StreamWriter file = File.CreateText(@"C:\\Users\\alanw\\OneDrive\\Documentos\\GitHub\\HostelAllocation\\HostelAllocationOptimization\\bin\\Debug\\netcoreapp2.0\\AutomatizatedTestsResults.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, results);
            }

            return results;
        }

        private static Dictionary<int, int> AssociatePersonToGroup(int numGroups, int[] groupsSizes)
        {
            Dictionary<int, int> personGroup = new Dictionary<int, int>();
            int person = 0;
            for(int i = 0; i < numGroups; i++)
            {
                for(int j = 0; j < groupsSizes[i]; j++)
                {
                    personGroup.Add(person, i);
                    person++;
                }
            }
            return personGroup;
        }

        public static List<DailyGroupsAllocationDTO> ListDailyGroupsAllocation(GRBVar[,,] x, GRBVar[,] y, double objVal)
        {
            int numGroups = x.GetLength(0);
            int numRooms = x.GetLength(1);

            List<DailyGroupsAllocationDTO> groupsAllocation = new List<DailyGroupsAllocationDTO>();

            for (int k = 0; k < x.GetLength(2); k++)
            {
                double[,] daily = new double[numGroups, numRooms];
                for (int i = 0; i < x.GetLength(0); i++)
                {
                    for (int j = 0; j < x.GetLength(1); j++)
                    {
                        daily[i, j] = x[i, j, k].X;
                    }
                }
                groupsAllocation.Add(new DailyGroupsAllocationDTO(daily, objVal));
            }

            return groupsAllocation;
        }

        public static List<DailyRoomAllocation> ListDailyRoomAllocation(GRBVar[,,] x, GRBVar[,] y, double objVal)
        {
            int numGroups = x.GetLength(0);
            int numRooms = x.GetLength(1);

            List<DailyRoomAllocation> roomsAllocation = new List<DailyRoomAllocation>();

            for (int k = 0; k < x.GetLength(2); k++)
            {
                DailyRoomAllocation dailyAllocation = new DailyRoomAllocation();
                int numGroupsSplits = 0;
                for (int i = 0; i < x.GetLength(0); i++)
                {
                    if (y[i, k].X == 1)
                        numGroupsSplits++;

                    for (int j = 0; j < x.GetLength(1); j++)
                    {
                        if(x[i, j, k].X == 1)
                        {
                            dailyAllocation.RoomGroupsAllocated.Add(new Tuple<int, int>(j, i));
                        }
                    }
                }
                dailyAllocation.NumGroupsSplits = numGroupsSplits;
                dailyAllocation.FuncObj = objVal;
                dailyAllocation.FillRoomAllocation(numRooms);
                roomsAllocation.Add(dailyAllocation);
            }

            return roomsAllocation;
        }

        public static List<DailyPeopleAllocation> ListDailyPeopleRoomAllocation(GRBVar[,,] x, GRBVar[,] z, double objVal)
        {
            int numGroups = x.GetLength(0);
            int numRooms = x.GetLength(1);

            List<DailyPeopleAllocation> roomsAllocation = new List<DailyPeopleAllocation>();

            for (int k = 0; k < x.GetLength(2); k++)
            {
                DailyPeopleAllocation dailyAllocation = new DailyPeopleAllocation();
                for (int i = 0; i < x.GetLength(0); i++)
                {
                    for (int j = 0; j < x.GetLength(1); j++)
                    {
                        if (x[i, j, k].X == 1)
                        {
                            dailyAllocation.RoomPeopleAllocation.Add(new Tuple<int, int>(j, i));
                        }
                    }
                }
                roomsAllocation.Add(dailyAllocation);
            }

            return roomsAllocation;
        }

        public static List<DailyRoomAllocation> ListDailyRoomAllocation(GRBVar[,,] x, GRBVar[,] y, GRBVar[,] z, 
            Dictionary<int,int> personToGroup, double objVal)
        {
            int numGroups = x.GetLength(0);
            int numRooms = x.GetLength(1);

            List<DailyRoomAllocation> roomsAllocation = new List<DailyRoomAllocation>();

            for (int k = 0; k < x.GetLength(2); k++)
            {
                DailyRoomAllocation dailyAllocation = new DailyRoomAllocation();
                double numGroupsSplits = 0;
                double numGroupsChanges = 0;
                for (int i = 0; i < x.GetLength(0); i++)
                {
                    if (y[personToGroup[i], k].X == 1)
                        numGroupsSplits += 1 / Convert.ToDouble(personToGroup.Where(p => p.Value == personToGroup[i]).Count());
                    if (z[personToGroup[i], k].X == 1)
                        numGroupsChanges += 1 / Convert.ToDouble(personToGroup.Where(p => p.Value == personToGroup[i]).Count());

                    for (int j = 0; j < x.GetLength(1); j++)
                    {
                        if (x[i, j, k].X == 1)
                        {
                            dailyAllocation.RoomGroupsAllocated.Add(new Tuple<int, int>(j, personToGroup[i]));
                        }
                    }
                }
                dailyAllocation.NumGroupsSplits = numGroupsSplits;
                dailyAllocation.NumGroupsChanges = numGroupsChanges;
                dailyAllocation.FuncObj = objVal;
                dailyAllocation.FillRoomAllocation(numRooms);
                if(k != 0)
                    FillGroupsEnteredLeft(dailyAllocation, roomsAllocation.Last());
                roomsAllocation.Add(dailyAllocation);
            }

            return roomsAllocation;
        }

        private static void FillGroupsEnteredLeft(DailyRoomAllocation dailyAllocation, DailyRoomAllocation previousAllocation)
        {
            var groups = dailyAllocation.RoomGroupsAllocated.Select(g => g.Item2).Distinct();
            var lastDayGroups = previousAllocation.RoomGroupsAllocated.Select(g => g.Item2).Distinct();
            List<int> groupsEntered = new List<int>();
            List<int> groupsLeft = new List<int>();
            foreach (var group in groups)
            {
                if(lastDayGroups.Any(g => g == group)) {
                    groupsEntered.Add(group);
                } else
                {
                    groupsLeft.Add(group);
                }
            }
            dailyAllocation.GroupsEntered = string.Join(";", groupsEntered);
            dailyAllocation.GroupsLeft = string.Join(";", groupsLeft);
        }
    }
}

