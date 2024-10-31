﻿using Reservation.Models;

namespace Reservation.Interfaces
{
    public interface IEquipmentRepository
    {
        Task<IEnumerable<Equipment>> GetAllEquipments();
        Task<Equipment> GetEquipmentById(int id);
        bool Save();
        bool Update(Equipment equipment);
        bool Add(Equipment equipment);
        bool Remove(Equipment equipment);

    }
}