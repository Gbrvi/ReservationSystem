﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservation.Interfaces;
using Reservation.Models;
using Reservation.ViewModel;

namespace Reservation.Controllers
{
    public class ReserveController : Controller
    {
        private readonly IReserveRepository _reserveRepository;
        private readonly IRoomRepository _roomRepository;
        public ReserveController(IReserveRepository reserveRepository, IRoomRepository roomRepository)
        {
            _reserveRepository = reserveRepository;
            _roomRepository = roomRepository;

        }

        [Authorize]
        public IActionResult Create(int? roomId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (roomId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var reserveVM = new CreateReserveViewModel
            {
                RoomId = roomId.Value,
                UserId = userId,
            };

            return View(reserveVM);
        }

        [HttpPost]
        public async Task<IActionResult> Create(RoomDetailViewModel? roomDetail)
        {

            if (!ModelState.IsValid)
            {
                // Se o modelo não for válido, obteremos os detalhes da sala e passaremos para a view novamente
                var room = await _roomRepository.GetByIdAsync(roomDetail.CreateReserveViewModel.RoomId);


                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }

                foreach (var error in ModelState)
                {
                    if (error.Value.Errors.Count > 0)
                    {
                        Console.WriteLine($"Erro no campo {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                }


                if (room == null)
                {
                    return View("Error"); // Se a sala não existir, mostra um erro
                }

                // Passa os detalhes da sala junto com o modelo de reserva de volta para a View de detalhes
                var roomDetailViewModel = new RoomDetailViewModel
                {
                    Room = room,
                    CreateReserveViewModel = roomDetail.CreateReserveViewModel
                };

                // Exibe a página de detalhes com os erros de validação
                return RedirectToAction("Detail", "Room", new { id = roomDetail.CreateReserveViewModel.RoomId });
            }

            var existingReserve = await _reserveRepository.GetReserveByRoomAndDateAsync(
                roomDetail.CreateReserveViewModel.RoomId, roomDetail.CreateReserveViewModel.ReserveDate,
                roomDetail.CreateReserveViewModel.ReserveStart, roomDetail.CreateReserveViewModel.ReserveEnd);

            if (existingReserve != null)
            {
                ModelState.AddModelError("CreateReserveViewModel.ReserveDate", "Já existe uma reserva para esta sala neste horário.");
                return View("Details", new RoomDetailViewModel
                {
                    Room = await _roomRepository.GetByIdAsync(roomDetail.CreateReserveViewModel.RoomId),
                    CreateReserveViewModel = roomDetail.CreateReserveViewModel
                });
            }

            var reserve = new Reserve
            {
                RoomId = roomDetail.CreateReserveViewModel.RoomId,
                UserId = roomDetail.CreateReserveViewModel.UserId,
                ReserveDate = roomDetail.CreateReserveViewModel.ReserveDate,
                ReserveStart = roomDetail.CreateReserveViewModel.ReserveStart,
                ReserveEnd = roomDetail.CreateReserveViewModel.ReserveEnd,
                ReserveStatus = roomDetail.CreateReserveViewModel.ReserveStatus,
                RentPrice = roomDetail.CreateReserveViewModel.RentPrice,
            };

            _reserveRepository.AddReserve(reserve);
            
            return RedirectToAction("Confirmation", "Reserve");


        }

        public IActionResult Confirmation()
        {
            return View();
        }

    }
}
