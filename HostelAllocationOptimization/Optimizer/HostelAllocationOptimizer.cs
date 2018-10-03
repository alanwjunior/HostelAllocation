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
                string path= "C:\\Users\\Alan.ALANJUNIORPC\\Desktop\\tcc\\TCC\\TCC2\\Gurobi\\Teste1\\gurobi\\bin\\Debug\\netcoreapp2.1\\" + jsonFileName;
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
                                    model.AddConstr((1 - x[i, j, k]) + x[i, j, k + 1] + y[i, k] >= 1, "Grupo " + i + " mudou de quarto dia " + k);
                                }
                                else if (groupsDemands.Any(d => d.Item1 == i && d.Item2 == k) && groupsDemands.Any(d => d.Item1 == i && d.Item2 == k + 1)) // O grupo tem demanda para o dia seguinte
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

                    if (model.Status == GRB.Status.INFEASIBLE)
                    {
                        throw new Exception("Infeasible model");
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
                //Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
                throw;
            }
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
                for (int i = 0; i < x.GetLength(0); i++)
                {
                    for (int j = 0; j < x.GetLength(1); j++)
                    {
                        if(x[i, j, k].X == 1)
                        {
                            dailyAllocation.RoomGroupsAllocated.Add(new Tuple<int, int>(j, i));
                        }
                    }
                }
                dailyAllocation.FillRoomAllocation(numRooms);
                roomsAllocation.Add(dailyAllocation);
            }

            return roomsAllocation;
        }
    }
}
