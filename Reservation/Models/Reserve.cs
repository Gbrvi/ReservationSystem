﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reservation.Models
{
    public class Reserve
    {
        [Key]
        public required int ReserveId { get; set; }
        [ForeignKey("User")]
        public required int UserId { get; set; }
        [ForeignKey("Room")]
        public required int RoomId { get; set; }
        [ForeignKey("Equipment")]
        public required int? EquipementId { get; set; }
        public required DateTime ReserveDate { get; set; }
        public required TimeOnly ReserveStart { get; set; }
        public required TimeOnly ReserveEnd { get; set; }
        public required string ReserveStatus { get; set; }
        public required float RentPrice { get; set; }
        public required string ReservePhotos { get; set; }
    }
}
