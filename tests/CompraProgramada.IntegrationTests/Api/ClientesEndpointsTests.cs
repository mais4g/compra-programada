using System.Net;
using System.Net.Http.Json;
using CompraProgramada.Application.DTOs.Requests;
using CompraProgramada.Application.DTOs.Responses;
using FluentAssertions;

namespace CompraProgramada.IntegrationTests.Api;

public class ClientesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ClientesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Adesao_ComDadosValidos_DeveRetornar201()
    {
        var request = new AdesaoRequest
        {
            Nome = "Teste Integration",
            Cpf = "99988877766",
            Email = "teste@email.com",
            ValorMensal = 3000m
        };

        var response = await _client.PostAsJsonAsync("/api/clientes/adesao", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AdesaoResponse>();
        body.Should().NotBeNull();
        body!.Nome.Should().Be("Teste Integration");
        body.Cpf.Should().Be("99988877766");
        body.ValorMensal.Should().Be(3000m);
        body.Ativo.Should().BeTrue();
        body.ContaGrafica.Should().NotBeNull();
        body.ContaGrafica!.Tipo.Should().Be("FILHOTE");
    }

    [Fact]
    public async Task Adesao_CpfDuplicado_DeveRetornar400()
    {
        var request = new AdesaoRequest
        {
            Nome = "Duplicado A",
            Cpf = "11111111111",
            Email = "dup@email.com",
            ValorMensal = 1000m
        };

        await _client.PostAsJsonAsync("/api/clientes/adesao", request);
        var response = await _client.PostAsJsonAsync("/api/clientes/adesao", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Saida_ClienteExistente_DeveRetornar200()
    {
        // Criar cliente
        var request = new AdesaoRequest
        {
            Nome = "Saida Test",
            Cpf = "22233344455",
            Email = "saida@email.com",
            ValorMensal = 1500m
        };
        var createResponse = await _client.PostAsJsonAsync("/api/clientes/adesao", request);
        var created = await createResponse.Content.ReadFromJsonAsync<AdesaoResponse>();

        // Sair
        var response = await _client.PostAsync($"/api/clientes/{created!.ClienteId}/saida", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SaidaResponse>();
        body!.Ativo.Should().BeFalse();
        body.DataSaida.Should().NotBeNull();
    }

    [Fact]
    public async Task Saida_ClienteInexistente_DeveRetornar404()
    {
        var response = await _client.PostAsync("/api/clientes/99999/saida", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AlterarValorMensal_ClienteExistente_DeveRetornar200()
    {
        // Criar cliente
        var adesao = new AdesaoRequest
        {
            Nome = "Valor Test",
            Cpf = "55566677788",
            Email = "valor@email.com",
            ValorMensal = 2000m
        };
        var createResponse = await _client.PostAsJsonAsync("/api/clientes/adesao", adesao);
        var created = await createResponse.Content.ReadFromJsonAsync<AdesaoResponse>();

        // Alterar valor
        var request = new AlterarValorMensalRequest { NovoValorMensal = 5000m };
        var response = await _client.PutAsJsonAsync(
            $"/api/clientes/{created!.ClienteId}/valor-mensal", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AlterarValorMensalResponse>();
        body!.ValorMensalAnterior.Should().Be(2000m);
        body.ValorMensalNovo.Should().Be(5000m);
    }
}