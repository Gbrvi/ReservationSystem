using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using Xunit;
using System;
using SeleniumExtras.WaitHelpers; // Adicione este using para ExpectedConditions

namespace Reservation.Tests.Unit
{
    public class RegisterTests : IDisposable
    {
        private readonly IWebDriver driver;
        private readonly WebDriverWait wait;

        private readonly string baseUrl = "http://localhost:5139";

        public RegisterTests()
        {
            driver = new FirefoxDriver();
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void Register_With_Existing_CPF_Should_Show_Error()
        {
            driver.Navigate().GoToUrl($"{baseUrl}/Account/Register");

            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("EmailAddress"))).SendKeys("teste3@duplicado.com");
            driver.FindElement(By.Id("Password")).SendKeys("SenhaForte1234!");
            driver.FindElement(By.Id("ConfirmPassword")).SendKeys("SenhaForte1234!");
            driver.FindElement(By.Id("Name")).SendKeys("Usuário Teste");
            driver.FindElement(By.Id("PhoneNumber")).SendKeys("11999999999");
            driver.FindElement(By.Id("CPF")).SendKeys("12345678901"); // CPF duplicado

            driver.FindElement(By.Id("Address")).SendKeys("Rua Exemplo, 123");

            var userTypeDropdownElement = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("UserType")));
            var userTypeDropdown = new SelectElement(userTypeDropdownElement);

            // Use SelectByValue para o UserType, como discutimos.
            // O valor '0' é uma suposição para "Comum" ou "Common".
            // Verifique o valor numérico exato do seu enum no HTML.
            wait.Until(ExpectedConditions.ElementIsVisible(By.XPath(".//option[@value='0']")));
            userTypeDropdown.SelectByValue("0");

            // Submete o formulário
            driver.FindElement(By.CssSelector("input[type='submit']")).Click();

            // --- AQUI ESTÁ A MUDANÇA PRINCIPAL PARA PEGAR A MENSAGEM DE ERRO ---
            // Tenta encontrar a mensagem de erro associada ao campo CPF
            try
            {
                // Espera por um SPAN com a classe 'text-danger' que seja um irmão do input CPF
                // ou um SPAN de validação específico para o CPF
                // Opção A: Mensagem de erro diretamente abaixo do campo CPF (validação de campo)
                var errorMessage = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("#CPF + .text-danger")));
                // OU, se houver um Span com o atributo data-valmsg-for="CPF"
                // var errorMessage = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("span[data-valmsg-for='CPF']")));

                Assert.True(errorMessage.Displayed, "A mensagem de erro do CPF não foi exibida.");
                Assert.Contains("CPF", errorMessage.Text);
            }
            catch (WebDriverTimeoutException)
            {
                // Se o erro de CPF não aparecer abaixo do campo, verifica se ele aparece no resumo de validação
                // (validation-summary) ou no TempData["Error"] no topo da página
                try
                {
                    var generalErrorMessage = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".alert.alert-danger")));
                    Assert.True(generalErrorMessage.Displayed, "A mensagem de erro geral (alert-danger) não foi exibida.");
                    Assert.Contains("CPF", generalErrorMessage.Text);
                }
                catch (WebDriverTimeoutException)
                {
                    // Se nenhuma das mensagens de erro foi encontrada, o teste falha
                    Assert.Fail("Nenhuma mensagem de erro de validação (CPF ou geral) foi encontrada após a submissão do formulário.");
                }
            }
        }

        public void Dispose()
        {
            driver.Quit();
        }
    }
}