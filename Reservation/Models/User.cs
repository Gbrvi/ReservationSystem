using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using Xunit;
using System;
using SeleniumExtras.WaitHelpers; // Certifique-se de que este using está presente

namespace Reservation.Tests.Unit
{
    public class ReservationTests : IDisposable // Renomeei a classe para ser mais genérica para testes de reserva
    {
        private readonly IWebDriver driver;
        private readonly WebDriverWait wait;

        private readonly string baseUrl = "http://localhost:5139"; // Sua URL base

        public ReservationTests()
        {
            driver = new FirefoxDriver();
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void NonMedicalUser_Cannot_ReserveRoom_ShowsErrorMessage()
        {
            // --- ARRANGE: Preparação ---

            // 1. Navegar para a página de login
            driver.Navigate().GoToUrl($"{baseUrl}/Account/Login");

            // 2. Fazer login com um usuário NÃO-MÉDICO
            // ATENÇÃO: SUBSTITUA ESTES DADOS POR UM USUÁRIO REAL QUE NÃO É MÉDICO NO SEU BD!
            string nonMedicalUserEmail = "usuario.comum@exemplo.com";
            string nonMedicalUserPassword = "SenhaComum123!";

            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("EmailAddress"))).SendKeys(nonMedicalUserEmail);
            driver.FindElement(By.Id("Password")).SendKeys(nonMedicalUserPassword);
            driver.FindElement(By.CssSelector("input[type='submit']")).Click(); // Clica no botão de login

            // Esperar o login ser processado e redirecionar para a página inicial (ou dashboard)
            // Você pode verificar a URL ou um elemento na página pós-login
            wait.Until(ExpectedConditions.UrlContains("/Home/Index") || ExpectedConditions.UrlContains("/Dashboard")); // Ajuste a URL esperada

            // 3. Navegar para a página de reserva de sala
            driver.Navigate().GoToUrl($"{baseUrl}/Reservation/Create"); // Ajuste o caminho para sua página de reserva

            // --- ACT: Ação ---

            // Tentar preencher e submeter o formulário de reserva
            // ATENÇÃO: SUBSTITUA ESTES SELECTORES PELOS IDS/NOMES REAIS DO SEU FORMULÁRIO DE RESERVA!
            // Exemplo: Selecionar uma sala
            // wait.Until(ExpectedConditions.ElementIsVisible(By.Id("RoomId"))).SendKeys("Sala A");
            // driver.FindElement(By.Id("Date")).SendKeys("2025-07-01"); // Exemplo de data
            // driver.FindElement(By.Id("Time")).SendKeys("10:00");     // Exemplo de hora

            // Submeter o formulário de reserva (assumindo que há um botão de submit)
            // driver.FindElement(By.CssSelector("input[type='submit'][value='Reservar']")).Click(); 
            // OU, se o formulário for de alguma outra forma, ajuste aqui.
            // Para este teste, se o usuário não pode reservar, ele talvez nem veja o formulário ou o botão.
            // O mais importante é a asserção.

            // --- ASSERT: Verificação ---

            // Verificar se a mensagem de erro esperada aparece
            // Opção 1: Mensagem de erro específica na página
            try
            {
                // Espera por uma mensagem de erro de validação ou de permissão
                // Ajuste o seletor CSS e o texto esperado com base na sua aplicação!
                var errorMessage = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".text-danger") /* ou .alert.alert-danger */));

                Assert.True(errorMessage.Displayed, "A mensagem de erro de permissão não foi exibida.");
                Assert.Contains("não é permitido", errorMessage.Text, StringComparison.OrdinalIgnoreCase, "O texto da mensagem de erro não é o esperado.");
                // OU, se for redirecionado para uma página de erro:
                // Assert.Contains("/AccessDenied", driver.Url, StringComparison.OrdinalIgnoreCase, "Não foi redirecionado para a página de acesso negado.");
            }
            catch (WebDriverTimeoutException)
            {
                Assert.Fail("Nenhuma mensagem de erro ou redirecionamento para acesso negado foi detectado.");
            }
        }

        public void Dispose()
        {
            driver.Quit();
        }
    }
}