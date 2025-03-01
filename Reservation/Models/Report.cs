﻿using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reservation.Models
{
    public class Report
    {
        [Key]
        public int ReportId { get; set; }
        [ForeignKey("User")]
        public required string UserId { get; set; }
        [ForeignKey("ReserveId")]
        public int ReserveId { get; set; }
        public string ReportTitle { get; set; }
        public string? ReportObservation { get; set; }
        public List<ReportFile> ReportArchives { get; set; } = new List<ReportFile>();
        public bool ReportBanStatus { get; set; }
        public DateOnly ReportDate { get; set; }
        public string ReportCreatedBy { get; set; }
    }
}
