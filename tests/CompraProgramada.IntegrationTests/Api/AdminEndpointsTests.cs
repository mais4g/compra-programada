using System.Net;
using System.Net.Http.Json;
using CompraProgramada.Application.DTOs.Requests;
using CompraProgramada.Application.DTOs.Responses;
using FluentAssertions;

namespace CompraProgramada.IntegrationTests.Api;

public class AdminEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AdminEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CadastrarCesta_ComDadosValidos_DeveRetornar201()
    {
        var request = new CestaRequest
        {
            Nome = "Top Five Integration",
            Itens = new List<CestaItemRequest>
            {
                new() { Ticker = "PETR4", Percentual = 30 },
                new() { Ticker = "VALE3", Percentual = 25 },
                new() { Ticker = "ITUB4", Percentual = 20 },
                new() { Ticker = "BBDC4", Percentual = 15 },
                new() { Ticker = "WEGE3", Percentual = 10 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/admin/cesta", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CestaResponse>();
        body.Should().NotBeNull();
        body!.Ativa.Should().BeTrue();
        body.Itens.Should().HaveCount(5);
    }

    [Fact]
    public async Task ObterCestaAtual_SemCesta_DeveRetornar404()
    {
        var response = await _client.GetAsync("/api/admin/cesta/atual");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ObterCestaAtual_ComCesta_DeveRetornar200()
    {
        var request = new CestaRequest
        {
            Nome = "Top Five Atual",
            Itens = new List<CestaItemRequest>
            {
                new() { Ticker = "PETR4", Percentual = 30 },
                new() { Ticker = "VALE3", Percentual = 25 },
                new() { Ticker = "ITUB4", Percentual = 20 },
                new() { Ticker = "BBDC4", Percentual = 15 },
                new() { Ticker = "WEGE3", Percentual = 10 }
            }
        };
        await _client.PostAsJsonAsync("/api/admin/cesta", request);

        var response = await _client.GetAsync("/api/admin/cesta/atual");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
