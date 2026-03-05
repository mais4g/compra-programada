using System.Net;
using System.Net.Http.Json;
using CompraProgramada.Application.DTOs.Requests;
using CompraProgramada.Application.DTOs.Responses;
using FluentAssertions;

namespace CompraProgramada.IntegrationTests.Api;

public class MotorEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MotorEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ExecutarCompra_SemCesta_DeveRetornar404()
    {
        var request = new ExecutarCompraRequest { DataReferencia = new DateTime(2026, 3, 5) };

        var response = await _client.PostAsJsonAsync("/api/motor/executar-compra", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RebalancearPorDesvio_ClienteInexistente_DeveRetornar200()
    {
        // Rebalanceamento por desvio para cliente inexistente retorna OK (graceful)
        var response = await _client.PostAsync("/api/motor/rebalancear-desvio/99999", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RebalancearPorDesvio_ClienteExistente_DeveRetornar200()
    {
        var created = await CriarClienteAsync("Rebal Test", "88899900011");

        var response = await _client.PostAsync($"/api/motor/rebalancear-desvio/{created.ClienteId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MensagemResponse>();
        body.Should().NotBeNull();
        body!.Mensagem.Should().Contain("Rebalanceamento");
    }

    [Fact]
    public async Task ConsultarCarteira_ClienteExistente_DeveRetornar200()
    {
        var created = await CriarClienteAsync("Carteira Test", "11122233300");

        var response = await _client.GetAsync($"/api/clientes/{created.ClienteId}/carteira");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CarteiraResponse>();
        body.Should().NotBeNull();
        body!.Nome.Should().Be("Carteira Test");
        body.Ativos.Should().NotBeNull();
    }

    [Fact]
    public async Task ConsultarCarteira_ClienteInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync("/api/clientes/99998/carteira");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConsultarRentabilidade_ClienteExistente_DeveRetornar200()
    {
        var created = await CriarClienteAsync("Rentab Test", "44455566600");

        var response = await _client.GetAsync($"/api/clientes/{created.ClienteId}/rentabilidade");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RentabilidadeResponse>();
        body.Should().NotBeNull();
        body!.Nome.Should().Be("Rentab Test");
        body.HistoricoAportes.Should().NotBeNull();
        body.EvolucaoCarteira.Should().NotBeNull();
    }

    [Fact]
    public async Task ConsultarRentabilidade_ClienteInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync("/api/clientes/99997/rentabilidade");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<AdesaoResponse> CriarClienteAsync(string nome, string cpf)
    {
        var request = new AdesaoRequest
        {
            Nome = nome,
            Cpf = cpf,
            Email = $"{cpf}@test.com",
            ValorMensal = 3000m
        };
        var response = await _client.PostAsJsonAsync("/api/clientes/adesao", request);
        return (await response.Content.ReadFromJsonAsync<AdesaoResponse>())!;
    }

    // DTO helper para deserializar resposta simples
    private record MensagemResponse
    {
        public string Mensagem { get; init; } = "";
    }
}
