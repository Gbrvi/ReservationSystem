﻿using System.ComponentModel.DataAnnotations;

namespace Reservation.ViewModel
{
    public class UserManagmentViewModel
    {

        public string UserId { get; set; }
        public List<string> Roles { get; set; }
        public string CPF { get; set; }
        public string Email { get; set; }
        public string? CRMNumber { get; set; }
        public string? OABNumber { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? BannedUntil { get; set; }

        public List<string>? RolesUserLogged { get; set; }
        public string? BanReason { get; set; }

    }
}
