﻿using HostelAllocationOptimization.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using HostelAllocationOptimization.Optimizer;
using Newtonsoft.Json;
using System.IO;

namespace HostelAllocationOptimization.TestFactory
{
    public static class TestFactory
    {
        #region Second Model
        public static HostelAllocationDTO CreateRandomTest(string fileName)
        {
            Dictionary<int, int> groupsSizes = new Dictionary<int, int>();
            HostelAllocationDTO hostelAllocation = new HostelAllocationDTO();
            hostelAllocation.InitialAllocation = new List<Tuple<int, int>>();
            hostelAllocation.GroupsDemands = new List<Tuple<int, int>>();

            var random = new Random();
            hostelAllocation.NumDays = random.Next(10, 20);
            //hostelAllocation.NumDays = 10;
            int hostelCapacity = random.Next(20, 50);
            //int hostelCapacity = 30;

            CreateRooms(hostelAllocation, hostelCapacity);

            hostelCapacity = hostelAllocation.RoomCapacity.Sum();

            SetInitialHostelAllocation(groupsSizes, hostelAllocation);

            for (var day = 1; day <= hostelAllocation.NumDays; day++)
            {
                DesallocateGroupsByDay(hostelAllocation, groupsSizes, day);
                var dailyAllocatedGroups = hostelAllocation.GroupsDemands.Where(d => d.Item2 == day).Select(d => d.Item1).ToList();
                int actualHostelCapacicity = dailyAllocatedGroups.Select(g => groupsSizes[g]).Sum();

                while (actualHostelCapacicity < hostelCapacity)
                {
                    int groupIndex = groupsSizes.Count;
                    var actualHostelAllocation = hostelAllocation.GroupsDemands.Where(g => g.Item2 == day).Select(g => groupsSizes[g.Item1]).Sum();
                    var maxGroupSize = hostelCapacity - actualHostelAllocation > 10 ? 10 : hostelCapacity - actualHostelAllocation;
                    if(maxGroupSize >= 1)
                    {
                        var groupSize = random.Next(1, maxGroupSize);
                        groupsSizes.Add(groupIndex, groupSize);
                        hostelAllocation.GroupsDemands.Add(new Tuple<int, int>(groupIndex, day));
                        actualHostelCapacicity += groupSize;
                    }
                }
            }
            hostelAllocation.GroupsSizes = groupsSizes.Select(g => g.Value).ToArray();

            using (StreamWriter file = File.CreateText(@"C:\\Users\\alanw\\OneDrive\\Documentos\\GitHub\\HostelAllocation\\HostelAllocationOptimization\\bin\\Debug\\netcoreapp2.0\\" + fileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, hostelAllocation);
            }

            return hostelAllocation;
        }

        private static int DailyAllocation(HostelAllocationDTO hostelAllocation, int day)
        {
            return hostelAllocation.GroupsDemands.Where(g => g.Item2 == day).Select(g => g.Item1).Sum();
        }

        private static void CreateRooms(HostelAllocationDTO hostelAllocation, int hostelCapacity)
        {
            var random = new Random();
            hostelAllocation.NumRooms = random.Next(5, 10);
            List<int> roomsCapacity = new List<int>();

            for(var room = 0; room < hostelAllocation.NumRooms; room++)
            {
                if(room != hostelAllocation.NumRooms - 1)
                {
                    var hostelActualCapacity = roomsCapacity.Sum();
                    var maxRoomSize = hostelCapacity - hostelActualCapacity > 10 + hostelAllocation.NumRooms - room ? 
                        10 : hostelCapacity - hostelActualCapacity - hostelAllocation.NumRooms + room + 1;
                    var roomSize = random.Next(1, maxRoomSize);
                    roomsCapacity.Add(roomSize);
                }
                else
                {
                    var freeSpaces = hostelCapacity - roomsCapacity.Sum();
                    var roomSize = freeSpaces > 10 ? 10 : freeSpaces;
                    roomsCapacity.Add(roomSize);
                }
            }

            hostelAllocation.RoomCapacity = roomsCapacity.ToArray();
        }

        private static void SetInitialHostelAllocation(Dictionary<int, int> groupsSizes, HostelAllocationDTO hostelAllocation)
        {
            var random = new Random();
            var firstDayAllocation = random.Next(hostelAllocation.RoomCapacity.Sum() / 10, hostelAllocation.RoomCapacity.Sum());
            var availableRoomsSpaces = hostelAllocation.RoomCapacity
                    .Select(r => r - hostelAllocation.InitialAllocation.Where(i => i.Item2 == r).Select(i => groupsSizes[i.Item1]).Sum()).ToList();

            while (firstDayAllocation > 0 && availableRoomsSpaces.Sum() >= firstDayAllocation && availableRoomsSpaces.Any(room => room > 0))
            {
                var allocatedRooms = hostelAllocation.InitialAllocation.ToList();
                var randomGroupSize = random.Next(1, availableRoomsSpaces.Max() - 1);
                var room = availableRoomsSpaces.Where(r => r >= randomGroupSize).First();
                var roomIndex = availableRoomsSpaces.IndexOf(room);
                var group = groupsSizes.Count;
                groupsSizes.Add(group, randomGroupSize);
                hostelAllocation.InitialAllocation.Add(new Tuple<int,int>(group, roomIndex));
                availableRoomsSpaces[roomIndex] -= randomGroupSize;
                firstDayAllocation -= randomGroupSize;
                //availableRoomsSpaces = hostelAllocation.RoomCapacity
                //    .Select(r => r - hostelAllocation.InitialAllocation.Where(i => i.Item2 == r).Select(i => groupsSizes[i.Item1]).Sum()).ToList();
            }
        }

        private static void DesallocateGroupsByDay(HostelAllocationDTO hostelAllocation, Dictionary<int, int> groupsSizes, int day)
        {
            var random = new Random();
            var previousGroupsAllocated = hostelAllocation.GroupsDemands.Where(d => d.Item2 == day - 1).Select(d => d.Item1).ToList();
            var numGroupsToLeft = random.Next(0, previousGroupsAllocated.Count);
            for(var i = 0; i < numGroupsToLeft; i++)
            {
                var groupToLeft = random.Next(0, previousGroupsAllocated.Count - 1);
                previousGroupsAllocated.RemoveAt(groupToLeft);
            }
            foreach(var group in previousGroupsAllocated)
            {
                hostelAllocation.GroupsDemands.Add(new Tuple<int, int>(group, day));
            }
        }
        #endregion
        #region First Model
        //public static List<DailyRoomAllocation> CreateRandomTestFile(string fileName)
        //{
        //    Random random = new Random();
        //    int numRooms = random.Next(4, 10);
        //    int numDays = 2;
        //    int hostelCapacity = random.Next(15, 30);
        //    int numGroups = random.Next(6, 10);

        //    int[] roomsCapacity = GetRandomRoomsCapacity(numRooms, hostelCapacity);
        //    int[] groupsSizes = GetRandomGroupsSizes(numGroups, hostelCapacity, roomsCapacity.Max());

        //    HostelAllocationDTO hostelAllocation = new HostelAllocationDTO();
        //    hostelAllocation.NumDays = numDays + 1;
        //    hostelAllocation.NumRooms = numRooms;
        //    hostelAllocation.GroupsSizes = groupsSizes;
        //    hostelAllocation.RoomCapacity = roomsCapacity;

        //    SetRandomInitialAllocation(hostelAllocation, hostelCapacity);
        //    SetRandomHostelDemand(hostelAllocation, numDays);

        //    if(!ValidateTest(hostelAllocation, numDays, hostelCapacity))
        //    {
        //        throw new Exception("Invalid Test");
        //    }

        //    using (StreamWriter file = File.CreateText(@"C:\\Users\\Alan.ALANJUNIORPC\\Desktop\\tcc\\TCC\\TCC2\\HostelAllocationOptimization\\API\\HostelAllocationOptimization\\HostelAllocationOptimization\\bin\Debug\\netcoreapp2.0\\Testes\\" + fileName))
        //    {
        //        JsonSerializer serializer = new JsonSerializer();
        //        //serialize object directly into file stream
        //        serializer.Serialize(file, hostelAllocation);
        //    }

        //    return HostelAllocationOptimizer.Optimize(hostelAllocation);
        //}

        //private static bool ValidateTest(HostelAllocationDTO hostelAllocation, int numDays, int hostelCapacity)
        //{
        //    if (!IsInitialAllocationValid(hostelAllocation))
        //    {
        //        return false;
        //    } else
        //    {
        //        // Criar teste para validar demandas
        //        return true;
        //    }
        //}

        //private static bool IsInitialAllocationValid(HostelAllocationDTO hostelAllocation)
        //{
        //    bool isTestValid = true;
        //    var demands = hostelAllocation.InitialAllocation;
        //    var roomsIds = hostelAllocation.InitialAllocation.Select(a => a.Item2).ToList();
        //    foreach (var roomId in roomsIds)
        //    {
        //        int allocation = 0;
        //        var groups = hostelAllocation.InitialAllocation.Where(g => g.Item2 == roomId).Select(g => g.Item1).ToList();
        //        foreach (var group in groups)
        //        {
        //            allocation += hostelAllocation.GroupsSizes[group];
        //        }
        //        if (allocation > hostelAllocation.RoomCapacity[roomId])
        //            return false;
        //    }
        //    return isTestValid;
        //}

        //public static int[] GetRandomRoomsCapacity(int numRooms, int hostelCapacity)
        //{
        //    Random random = new Random();
        //    List<int> roomsCapacity = new List<int>();
        //    for(int i = 0; i < numRooms; i++)
        //    {
        //        int maxRoomSize = hostelCapacity - roomsCapacity.Sum() > 0 ? hostelCapacity - roomsCapacity.Sum() : 1;
        //        roomsCapacity.Add(random.Next(1, 6));
        //    }

        //    return roomsCapacity.ToArray();
        //}

        //public static int[] GetRandomGroupsSizes(int numGroups, int hostelCapacity, int maxRoomSize)
        //{
        //    Random random = new Random();
        //    List<int> groupsSizes = new List<int>();

        //    for (int i = 0; i < numGroups; i++) {
        //        groupsSizes.Add(random.Next(1, maxRoomSize));
        //    }

        //    return groupsSizes.ToArray();
        //}

        //public static void SetRandomInitialAllocation(HostelAllocationDTO hostelAllocation, int hostelCapacity)
        //{
        //    Random random = new Random();

        //    hostelAllocation.InitialAllocation = new List<Tuple<int, int>>();

        //    List<int> roomsFreeBeds = ListRoomAllocation(hostelAllocation.RoomCapacity);

        //    int numInitialGroups = random.Next(1, hostelAllocation.GroupsSizes.Count());
        //    List<int> groupsSizes = hostelAllocation.GroupsSizes.ToList();
        //    List<int> groupsAllocation = hostelAllocation.GroupsSizes.ToList();

        //    for (int i = 0; i < numInitialGroups; i++)
        //    {
        //        int groupIndex = random.Next(0, groupsSizes.Count() - 1);
        //        if(roomsFreeBeds.Any(beds => beds >= groupsSizes[groupIndex]))
        //        {
        //            var freeRoomBeds = roomsFreeBeds.Where(beds => beds >= groupsSizes[groupIndex]).First();
        //            int roomIndex = roomsFreeBeds.IndexOf(freeRoomBeds);
        //            if (roomIndex != -1)
        //            {
        //                roomsFreeBeds[roomIndex] = freeRoomBeds - groupsSizes[groupIndex];
        //                hostelAllocation.InitialAllocation.Add(new Tuple<int, int>(groupsAllocation.IndexOf(groupsSizes[groupIndex]), roomIndex));
        //                groupsAllocation[groupsAllocation.IndexOf(groupsSizes[groupIndex])] = 0;
        //                groupsSizes.RemoveAt(groupIndex);
        //            }
        //        }
        //    }
        //}

        //public static void SetRandomHostelDemand(HostelAllocationDTO hostelAllocation, int numDays)
        //{
        //    hostelAllocation.GroupsDemands = new List<Tuple<int, int>>();
        //    SetFirstDayDemand(hostelAllocation);
        //    for(int day = 2; day <= numDays; day++)
        //    {
        //        SetDailyHostelDemand(hostelAllocation, day);
        //    }
            
        //    //for(int i = 0; i < numDays; i++)
        //    //{
        //    //    SetDailyDemand();
        //    //}
        //    //SetDemands();
        //    //for (int i = 0; i < numDays; i++)
        //    //{
        //    //    List<int> freeRoomSpaces = ListRoomAllocation(hostelAllocation.GroupsSizes);
        //    //    List<int> notAllocatedGroupsSizes = ListRoomAllocation(hostelAllocation.RoomCapacity);
        //    //    List<int> allocatedGroups = new List<int>();
        //    //    while (freeRoomSpaces.Any(r => r > 0) && notAllocatedGroupsSizes.Count > 0 &&
        //    //        notAllocatedGroupsSizes.Any(g => freeRoomSpaces.Any(r => r >= g) && g > 0))
        //    //    {
        //    //        Random random = new Random();
        //    //        var randomGroupSizes = notAllocatedGroupsSizes.Where(g => freeRoomSpaces.Any(r => r >= g) && g > 0).ToList();
        //    //        var randomGroupSizeIndex = random.Next(0, randomGroupSizes.Count);
        //    //        var randomGroupSize = randomGroupSizes[randomGroupSizeIndex];

        //    //        var randomRooms = freeRoomSpaces.Where(r => r >= randomGroupSize).ToList();
        //    //        var randomRoomSizeIndex = random.Next(1, randomRooms.Count);
        //    //        var randomRoom = randomRooms[randomRoomSizeIndex];

        //    //        int groupIndex = notAllocatedGroupsSizes.IndexOf(randomGroupSize);
        //    //        int roomIndex = freeRoomSpaces.IndexOf(randomRoom);
        //    //        if (groupIndex != -1)
        //    //        {
        //    //            freeRoomSpaces[roomIndex] -= randomGroupSize;
        //    //            notAllocatedGroupsSizes[groupIndex] = 0;
        //    //        }
        //    //        hostelAllocation.GroupsDemands.Add(new Tuple<int, int>(groupIndex, i));
        //    //    }
        //    //}
        //}

        //private static void SetDailyHostelDemand(HostelAllocationDTO hostelAllocation, int day)
        //{
        //    Random random = new Random();
        //    var previousDayDemands = hostelAllocation.GroupsDemands.Where(d => d.Item2 == day - 1).ToList();
        //    var groupsAllocated = previousDayDemands.Select(d => d.Item1).ToList();
        //    List<int> freeRoomSpaces = ListRoomAllocation(hostelAllocation.RoomCapacity);
        //    Dictionary<int, int> dailyDemands = new Dictionary<int, int>();
            
        //    // Sorteia um grupo para desalocar no dia
        //    int idxGroupToDesallocate = random.Next(0, groupsAllocated.Count - 1);
        //    groupsAllocated.RemoveAt(idxGroupToDesallocate);

        //    // Tamanhos de Grupos ordenado do maior pro menor
        //    Dictionary<int, int> sortedGroupsAllocatedSizes = new Dictionary<int, int>();
        //    Dictionary<int, int> sortedGroupsNotAllocatedSizes = new Dictionary<int, int>();
        //    for (int i = 0; i < hostelAllocation.GroupsSizes.Count(); i++)
        //    {
        //        if (!groupsAllocated.Contains(i))
        //        {
        //            sortedGroupsNotAllocatedSizes.Add(i, hostelAllocation.GroupsSizes[i]);
        //        } else
        //        {
        //            sortedGroupsAllocatedSizes.Add(i, hostelAllocation.GroupsSizes[i]);
        //        }
        //    }
        //    sortedGroupsAllocatedSizes.OrderByDescending(s => s.Value);
        //    sortedGroupsNotAllocatedSizes.OrderByDescending(s => s.Value);

        //    //Preenche os grupos já alocados
        //    foreach (var groupSize in sortedGroupsAllocatedSizes)
        //    {
        //        if (freeRoomSpaces.Any(room => room >= groupSize.Value))
        //        {
        //            int roomSpaces = freeRoomSpaces.Where(room => room >= groupSize.Value).First();
        //            int roomIndex = freeRoomSpaces.IndexOf(roomSpaces);
        //            if (roomIndex != -1)
        //            {
        //                hostelAllocation.GroupsDemands.Add(new Tuple<int, int>(groupSize.Key, day));
        //                freeRoomSpaces[roomIndex] -= groupSize.Value;
        //            }
        //        }
        //    }

        //    // Tenta inserir novos grupos no hostel
        //    if(freeRoomSpaces.Any(room => sortedGroupsAllocatedSizes.Any(s => s.Value <= room)))
        //    {
        //        foreach(var groupSize in sortedGroupsNotAllocatedSizes)
        //        {
        //            if (freeRoomSpaces.Any(room => room >= groupSize.Value))
        //            {
        //                int roomSpaces = freeRoomSpaces.Where(room => room >= groupSize.Value).First();
        //                int roomIndex = freeRoomSpaces.IndexOf(roomSpaces);
        //                if (roomIndex != -1)
        //                {
        //                    hostelAllocation.GroupsDemands.Add(new Tuple<int, int>(groupSize.Key, day));
        //                    freeRoomSpaces[roomIndex] -= groupSize.Value;
        //                }
        //            }
        //        }
        //    }
        //}

        //private static void SetFirstDayDemand(HostelAllocationDTO hostelAllocation)
        //{
        //    List<int> freeRoomSpaces = ListRoomAllocation(hostelAllocation.RoomCapacity);
        //    var initialsRoomFilled = hostelAllocation.InitialAllocation.Select(h => h.Item2);
        //    // Atualiza alocação do primeiro dia conforme alocação anterior
        //    foreach (var room in initialsRoomFilled)
        //    {
        //        var groups = hostelAllocation.InitialAllocation.Where(g => g.Item2 == room).Select(h => h.Item1);
        //        foreach (var group in groups)
        //        {
        //            freeRoomSpaces[room] -= hostelAllocation.GroupsSizes[group];
        //            hostelAllocation.GroupsDemands.Add(new Tuple<int, int>(group, 1));
        //        }
        //    }

        //    // Tamanhos de Grupos ordenado do maior pro menor
        //    Dictionary<int, int> sortedGroupsSizes = new Dictionary<int, int>();
        //    var allocatedGroups = hostelAllocation.InitialAllocation.Select(g => g.Item1).ToList();
        //    for (int i = 0; i < hostelAllocation.GroupsSizes.Count(); i++)
        //    {
        //        if(!allocatedGroups.Contains(i))
        //            sortedGroupsSizes.Add(i, hostelAllocation.GroupsSizes[i]);
        //    }
        //    sortedGroupsSizes.OrderByDescending(s => s.Value);

        //    //Preenche o máximo de pessoas que der, começando pelos grupos maiores
        //    foreach (var groupSize in sortedGroupsSizes)
        //    {
        //        if(freeRoomSpaces.Any(room => room >= groupSize.Value)) {
        //            int roomSpaces = freeRoomSpaces.Where(room => room >= groupSize.Value).First();
        //            int roomIndex = freeRoomSpaces.IndexOf(roomSpaces);
        //            if (roomIndex != -1)
        //            {
        //                hostelAllocation.GroupsDemands.Add(new Tuple<int, int>(groupSize.Key, 1));
        //            }
        //        }
        //    }

        //}

        ////public static void SetRandomHostelDemand(HostelAllocationDTO hostelAllocation, int numDays)
        ////{
        ////    hostelAllocation.GroupsDemands = new List<Tuple<int, int>>();
        ////    int hostelCapacity = hostelAllocation.GroupsSizes.Sum();
        ////    for (int i = 1; i < numDays; i++)
        ////    {
        ////        int dailyCapacity = hostelCapacity;
        ////        List<int> notAllocatedGroupsSizes = ListRoomAllocation(hostelAllocation.GroupsSizes);
        ////        List<int> allocatedGroups = new List<int>();
        ////        while (dailyCapacity > 0 && notAllocatedGroupsSizes.Count > 0 &&
        ////            notAllocatedGroupsSizes.Any(g => g <= dailyCapacity && g > 0))
        ////        {
        ////            Random random = new Random();
        ////            var randomGroupSize = notAllocatedGroupsSizes.Where(g => g <= dailyCapacity && g > 0).First();

        ////            int groupIndex = notAllocatedGroupsSizes.IndexOf(randomGroupSize);
        ////            if(groupIndex != -1)
        ////            {
        ////                dailyCapacity -= randomGroupSize;
        ////                notAllocatedGroupsSizes[groupIndex] = 0;
        ////            }
        ////            hostelAllocation.GroupsDemands.Add(new Tuple<int, int>(groupIndex, i));
        ////        }
        ////    }
        ////}

        //private static void AddGroups(HostelAllocationDTO hostelAllocation, int numGroupsToAdd, List<int> groups, int day)
        //{
        //    for (int i = 0; i < numGroupsToAdd; i++)
        //    {
        //        Random random = new Random();
        //        var groupIndex = random.Next(0, groups.Count());
        //        if (groupIndex != -1)
        //        {
        //            var groupdDemand = hostelAllocation.GroupsDemands.Where(g => g.Item1 == groupIndex && g.Item2 == day).First();
        //            var groupDemandIndex = hostelAllocation.GroupsDemands.IndexOf(groupdDemand);
        //            hostelAllocation.GroupsDemands.RemoveAt(groupDemandIndex);
        //            groups.RemoveAt(groupIndex);
        //        }
        //    }
        //}

        //private static void DeleteGroups(HostelAllocationDTO hostelAllocation, int numGroupsToDesallocate, List<int> groups, int day)
        //{
        //    for (int i = 0; i < numGroupsToDesallocate; i++)
        //    {
        //        Random random = new Random();
        //        var groupIndex = random.Next(0, groups.Count());
        //        if(groupIndex != -1)
        //        {
        //            var groupdDemand = hostelAllocation.GroupsDemands.Where(g => g.Item1 == groupIndex && g.Item2 == day).First();
        //            var groupDemandIndex = hostelAllocation.GroupsDemands.IndexOf(groupdDemand);
        //            hostelAllocation.GroupsDemands.RemoveAt(groupDemandIndex);
        //            groups.RemoveAt(groupIndex);
        //        }
        //    }
        //}

        //private static List<int> ListRoomAllocation(int[] roomsSizes)
        //{
        //    List<int> roomAllocation = new List<int>();
        //    for(var i = 0; i < roomsSizes.Count(); i++)
        //    {
        //        roomAllocation.Add(roomsSizes[i]);
        //    }
        //    return roomAllocation;
        //}
        #endregion
    }
}

