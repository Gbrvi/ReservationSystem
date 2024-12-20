﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reservation.Interfaces;
using Reservation.Models;
using Reservation.Repository;
using Reservation.ViewModel;
using System.Security.Claims;

namespace Reservation.Controllers
{
    public class ReserveController : Controller
    {
        private readonly IReserveRepository _reserveRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IReserveService _reserveService;
        private readonly IEquipmentRepository _equipmentRepository;
        private readonly IUserManagmenteRepository _userRepository;
        public ReserveController(IReserveRepository reserveRepository, IRoomRepository roomRepository, IReserveService reserveService, IEquipmentRepository equipmentRepository, IUserManagmenteRepository userRepository)
        {
            _reserveRepository = reserveRepository;
            _roomRepository = roomRepository;
            _reserveService = reserveService;
            _equipmentRepository = equipmentRepository;
            _userRepository = userRepository;

        }

        [Authorize]
        public IActionResult Create(int roomId)
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
                RoomId = roomId,
                UserId = userId,
            };

            return View(reserveVM);
        }

        [HttpPost]
        public async Task<IActionResult> Create(RoomDetailViewModel roomDetail)
        {
            if (!ModelState.IsValid)
            {
                // Se o modelo não for válido, obteremos os detalhes da sala e passaremos para a view novamente
                var room = await _roomRepository.GetByIdAsync(roomDetail.CreateReserveViewModel.RoomId);


                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }


                if (room == null)
                {
                    return View("Error"); // Se a sala não existir, mostra um erro
                }


                var user = roomDetail.CreateReserveViewModel.UserId;

                // Passa os detalhes da sala junto com o modelo de reserva de volta para a View de detalhes
                var roomDetailViewModel = new RoomDetailViewModel
                {
                    Room = room,
                    CreateReserveViewModel = roomDetail.CreateReserveViewModel,
                    Reservations = await _reserveRepository.GetReservesByRoomIdAsync(roomDetail.CreateReserveViewModel.RoomId),
                    RoomEquipments = roomDetail.RoomEquipments,


                };

                // Exibe a página de detalhes com os erros de validação
                TempData["ErrorMessage"] = "Model State Invalido";
                return RedirectToAction("Detail", "Room", new { id = roomDetail.CreateReserveViewModel.RoomId });
            }

            // Criei um reserveService para lidar com possíveis calculos/erros/validações que nao exigem buscar no banco de dados (No qual apenas o ReserveRepository faz essa função).
            // Para fazer, implementei uma interface e criei um reserveService onde contem todos as operaçõeos
            var userId = roomDetail.CreateReserveViewModel.UserId;

            if (await _reserveService.IsUserBanned(userId))
            {
                // Pegando o motivo do banimento para exibição
                var user = await _userRepository.GetUserByIdAsync(userId);

                TempData["ErrorMessage"] = "Você está banido por: " + (user.BanReason ?? "Motivo não informado.") + " Você será desbanido em: " + (user.BannedUntil);

                return RedirectToAction("Index", "Room");
            }

            if (!_reserveService.IsValidBusinessHours(roomDetail.CreateReserveViewModel.ReserveStart, roomDetail.CreateReserveViewModel.ReserveEnd))
            {
                TempData["ErrorMessage"] = "Os horários de reserva devem estar entre 08:00 e 20:00.";
                return RedirectToAction("Detail", "Room", new { id = roomDetail.CreateReserveViewModel.RoomId });

            }


            if (!_reserveService.IsValidReserveTime(roomDetail.CreateReserveViewModel.ReserveStart, roomDetail.CreateReserveViewModel.ReserveEnd))
            {
                TempData["ErrorMessage"] = "O horario de inicio deve ser anterior ao horario do final. ";
                return RedirectToAction("Detail", "Room", new { id = roomDetail.CreateReserveViewModel.RoomId });

            }


            if (!_reserveService.IsValidReserveDate(roomDetail.CreateReserveViewModel.ReserveDate))
            {

                TempData["ErrorMessage"] = "Selecione um dia válido";
                return RedirectToAction("Detail", "Room", new { id = roomDetail.CreateReserveViewModel.RoomId });

            }

            if (!_reserveService.IsReserveTimeOneHourAhead(roomDetail.CreateReserveViewModel.ReserveStart, roomDetail.CreateReserveViewModel.ReserveDate))
            {
                TempData["ErrorMessage"] = "Reserva precisa ter 1 hora de antecedencia";
                return RedirectToAction("Detail", "Room", new { id = roomDetail.CreateReserveViewModel.RoomId });

            }




            var existingReserve = await _reserveService.CheckExistingReservation(roomDetail.CreateReserveViewModel);

            if (existingReserve != null) // Modifiquei aqui, inclui o TempData ao inves do AddModelErro. (Até funcionava, mas nao tava exibindo msg pro usuario)
            {

                // ModelState.AddModelError("CreateReserveViewModel.ReserveDate", "Já existe uma reserva para esta sala neste horário.");
                TempData["ErrorMessage"] = "Já existe uma reserva para esta sala neste horário.";
                return RedirectToAction("Detail", "Room", new { id = roomDetail.CreateReserveViewModel.RoomId });
            }


            float RentPriceByHours = _reserveService.CalculatePriceByHours(roomDetail.CreateReserveViewModel.ReserveStart, roomDetail.CreateReserveViewModel.ReserveEnd,
               roomDetail.CreateReserveViewModel.RentPrice);

            var selectedEquipments = roomDetail.RoomEquipments.Where(e => e.IsSelected && e.EquipmentQuantity > 0).ToList();

            await _reserveRepository.ProcessExpiredReservations();

            await _equipmentRepository.BuyingEquipments(selectedEquipments);


            float totalRentPriceEquipments = 0;

            // Itere pelos equipamentos selecionados
            foreach (var equipment in selectedEquipments)
            {
                if (equipment.EquipmentQuantity > 0)
                {
                    totalRentPriceEquipments += equipment.EquipmentPrice * equipment.EquipmentQuantity.Value;
                }
            }

            // Calcule o preço total incluindo o aluguel base
            float totalPrice = RentPriceByHours + totalRentPriceEquipments;

            roomDetail.RoomPrice = RentPriceByHours;

            roomDetail.EquipmentPrice = totalRentPriceEquipments;

            roomDetail.CreateReserveViewModel.RentPrice = totalPrice;

            roomDetail.RoomEquipments = selectedEquipments;





            // Serializa o objeto roomDetail em uma string JSON
            var serializedModel = System.Text.Json.JsonSerializer.Serialize(roomDetail);

            // Adiciona a string JSON serializada como um cookie chamado "ReserveData" na resposta HTTP
            Response.Cookies.Append("ReserveData", serializedModel);

            // Redireciona para a página de confirmação
            return RedirectToAction("Confirmation", "Reserve");
        }




        public IActionResult Confirmation()
        {
            // Recupera os dados do cookie
            Request.Cookies.TryGetValue("ReserveData", out var serializedModel);

            if (string.IsNullOrEmpty(serializedModel))
            {
                return RedirectToAction("Index", "Home");
            }

            // Desserializa o JSON para o objeto
            var reserveVM = System.Text.Json.JsonSerializer.Deserialize<RoomDetailViewModel>(serializedModel);

            if (reserveVM == null || reserveVM.CreateReserveViewModel == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Ele não serializa o objeto Room, então precisamos buscar no banco de dados
            if (reserveVM.Room == null)
            {
                reserveVM.Room = _roomRepository.GetByIdAsync(reserveVM.CreateReserveViewModel.RoomId).Result;

                if (reserveVM.Room == null)
                {
                    return View("Error");
                }
            }
            // Carrega a view de confirmação
            return View(reserveVM);
        }





        [HttpPost]
        public async Task<IActionResult> Confirm(RoomDetailViewModel reserveVM)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Erro ao confirmar a reserva. Por favor, tente novamente.";
                return RedirectToAction("Confirmation", "Reserve", reserveVM);
            }

            // Validações finais antes de salvar
            var userId = reserveVM.CreateReserveViewModel.UserId;

            if (await _reserveService.IsUserBanned(userId))
            {
                // Pegando o motivo do banimento para exibição
                var user = await _userRepository.GetUserByIdAsync(userId);

                TempData["ErrorMessage"] = "Você está banido por: " + (user.BanReason ?? "Motivo não informado.") + " Você será desbanido em: " + (user.BannedUntil);
                return RedirectToAction("Index", "Room");

            }

            if (!_reserveService.IsValidBusinessHours(reserveVM.CreateReserveViewModel.ReserveStart, reserveVM.CreateReserveViewModel.ReserveEnd))
            {
                TempData["ErrorMessage"] = "Os horários de reserva devem estar entre 08:00 e 20:00.";
                return RedirectToAction("Detail", "Room", new { id = reserveVM.CreateReserveViewModel.RoomId });

            }


            if (!_reserveService.IsValidReserveTime(reserveVM.CreateReserveViewModel.ReserveStart, reserveVM.CreateReserveViewModel.ReserveEnd))
            {
                TempData["ErrorMessage"] = "O horario de inicio deve ser anterior ao horario do final. ";
                return RedirectToAction("Detail", "Room", new { id = reserveVM.CreateReserveViewModel.RoomId });

            }

            // Calcula o preço total e salva a reserva
            _reserveService.CreateReservation(reserveVM);

            // Redireciona para a página de sucesso (Index em Room)
            TempData["SuccessMessage"] = "Reserva confirmada com sucesso!";
            return RedirectToAction("Index", "Room");
        }

        public async Task<IActionResult> Cancel(int id)
        {
            var reserve = await _reserveRepository.GetReserveByIdAsync(id);

            if (reserve == null)
            {
                TempData["ErrorMessage"] = "Reserva Inválida";
                return RedirectToAction("UserReserves", "Reserve");

            }

            try
            {
                // Atualiza o Status da reserva no historico
                await _reserveService.UpdateStatusHistory(reserve);
                _reserveRepository.DeleteReserve(reserve); // Deleta a reserva
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Nao foi possivel deletar a reserva";
                return RedirectToAction("UserReserves", "Reserve");
            }

            return RedirectToAction("UserReserves", "Reserve");
        }


        public async Task<IActionResult> UserReserves()
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier); // Obtém o ID do usuário logado

            if (userID == null)
            {
                TempData["ErrorMessage"] = "Usuario Inválido";
                RedirectToAction("Index");
            }

            var reserveByUser = await _reserveRepository.GetReserveWhereStatusIsValidAsync(userID);

            return View(reserveByUser);



        }




    }

}
