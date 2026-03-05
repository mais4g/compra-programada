# Sistema de Compra Programada de Ações

Sistema desenvolvido para o desafio técnico Itaú Corretora. Permite que clientes adiram a um plano de compra recorrente de ações baseado em uma carteira recomendada "Top Five", com compra consolidada, distribuição proporcional, rebalanceamento automático e cálculo de IR.

## Stack Tecnológica

### Backend
- **.NET 8** (C#)
- **MySQL 8.0** (via Pomelo EntityFrameworkCore)
- **Apache Kafka** (Confluent)
- **MediatR** (CQRS - Commands/Queries)
- **Serilog** (Logs estruturados JSON + CorrelationId)
- **Docker Compose** para infraestrutura
- **GitHub Actions** (CI/CD)
- **xUnit + FluentAssertions + Moq** para testes

### Frontend
- **React 18** + **TypeScript**
- **Vite** (build tool)
- **React Router** (SPA navigation)
- **Axios** (HTTP client)

## Arquitetura

Clean Architecture com 4 camadas + frontend SPA:

```
src/
├── CompraProgramada.Domain          # Entidades, Enums, Interfaces, Exceptions
├── CompraProgramada.Application     # DTOs, Services, CQRS (Commands/Queries/Handlers)
├── CompraProgramada.Infrastructure  # EF Core, Repositories, Kafka, Parser COTAHIST
└── CompraProgramada.Api             # Controllers, Middleware, DI, Program.cs
frontend/                            # React SPA (TypeScript + Vite)
├── src/api/                         # Camada de serviço HTTP (Axios)
├── src/types/                       # Interfaces TypeScript espelhando DTOs do backend
├── src/pages/                       # Páginas: Adesão, Carteira, Rentabilidade, Admin, Motor
└── src/components/                  # Layout e navegação
tests/
├── CompraProgramada.UnitTests       # 65 testes unitários
└── CompraProgramada.IntegrationTests # 15 testes de integração
```

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (para o frontend)
- [Docker + Docker Compose](https://docs.docker.com/get-docker/)

## Como Executar

### 1. Subir infraestrutura (MySQL + Kafka)

```bash
docker-compose up -d
```

Serviços disponíveis:
| Serviço    | Porta |
|------------|-------|
| MySQL      | 3306  |
| Kafka      | 9092  |
| Zookeeper  | 2181  |

### 2. Executar a API

```bash
dotnet run --project src/CompraProgramada.Api
```

O banco de dados é criado automaticamente via migrations (auto-migrate no startup).

### 3. Executar o Frontend

```bash
cd frontend
npm install
npm run dev
```

O frontend estará disponível em **http://localhost:3000** (proxy automático para a API em localhost:5000).

### 4. Acessar Swagger

Abra no navegador: **http://localhost:5000** (ou a porta configurada)

### 5. Executar testes

```bash
dotnet test
```

## Endpoints da API

### Clientes (`/api/clientes`)

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/clientes/adesao` | Adesão ao produto (cria conta gráfica filhote) |
| POST | `/api/clientes/{id}/saida` | Saída do produto (custódia mantida) |
| PUT | `/api/clientes/{id}/valor-mensal` | Alterar valor mensal de aporte |
| GET | `/api/clientes/{id}/carteira` | Consultar carteira com P&L |
| GET | `/api/clientes/{id}/rentabilidade` | Rentabilidade detalhada por ativo |

### Administração (`/api/admin`)

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/admin/cesta` | Cadastrar/alterar cesta Top Five |
| GET | `/api/admin/cesta/atual` | Consultar cesta ativa |
| GET | `/api/admin/cesta/historico` | Histórico de cestas |
| GET | `/api/admin/conta-master/custodia` | Consultar resíduos na conta master |

### Motor de Compra e Rebalanceamento (`/api/motor`)

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/motor/executar-compra` | Executar compra programada (dias 5, 15, 25) |
| POST | `/api/motor/rebalancear-desvio/{clienteId}` | Rebalancear carteira por desvio de proporção |

## Regras de Negócio Principais

### Fluxo de Compra (Motor)
1. Cada cliente aporta **1/3 do valor mensal** por execução (dias 5, 15 e 25)
2. Valores são **consolidados** em uma única ordem de compra
3. Compra prioriza **lote padrão** (múltiplos de 100), resíduo vai para **fracionário**
4. Desconta saldo da **custódia master** antes de comprar
5. Distribui ações proporcionalmente com **TRUNCAR** (sem arredondamento)
6. Resíduos ficam na conta master para próxima execução
7. Atualiza **preço médio ponderado** da custódia filhote
8. Publica evento de **IR dedo-duro (0,005%)** via Kafka

### Rebalanceamento
- Disparado por **mudança de cesta** (vende removidos, compra adicionados)
- Disparado por **desvio > 5pp** na proporção dos ativos
- Vendas acima de **R$ 20.000/mês** geram IR de **20%** (publicado via Kafka)

### Ajuste de Dias Úteis
- Sábado -> Segunda-feira
- Domingo -> Segunda-feira

### Parser COTAHIST (B3)
- Arquivo posicional de 245 caracteres por linha
- Filtra CODBDI 02 (lote padrão) e 96 (fracionário)
- Filtra apenas mercado à vista (010) e fracionário (020)
- Ignora header (00), trailer (99) e linhas curtas
- Encoding ISO-8859-1

## Configuração

Arquivo `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=compra_programada;User=appuser;Password=apppass123;"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  },
  "CotacoesPath": "cotacoes"
}
```

Coloque os arquivos COTAHIST (.TXT) da B3 na pasta `cotacoes/` na raiz do projeto.

## Testes

80 testes (65 unitários + 15 integração):

| Projeto | Testes |
|---------|--------|
| CompraProgramada.UnitTests | 65 |
| CompraProgramada.IntegrationTests | 15 |
| **Total** | **80** |

Cobertura por camada: Application 97.1%, Domain 90.2%, Branch 85.9%, Method 88.7%.

Configuração: `coverlet.runsettings` exclui Migrations, DesignTimeFactory e SeedData.

```
Aprovado! - Com falha: 0, Aprovado: 80, Total: 80
```

## Tópicos Kafka

| Tópico | Evento |
|--------|--------|
| `ir-dedo-duro` | IR retido na fonte (0,005%) a cada compra distribuída |
| `ir-venda` | IR sobre ganho de capital em vendas (rebalanceamento) |

## Decisões Técnicas

### Arquitetura
- **Clean Architecture** com separação estrita de dependências: Domain não referencia nenhuma outra camada, Application depende apenas de Domain, Infrastructure implementa interfaces definidas em Domain/Application, e Api orquestra tudo via DI.
- **Domain-Driven Design (DDD)**: Entidades ricas com comportamento (ex: `Cliente.Desativar()`, `CestaTopFive.Desativar()`, `CustodiaFilhote.AtualizarPrecoMedio()`, `CustodiaFilhote.CalcularLucro()`), em vez de entidades anêmicas.
- **Repository + Unit of Work**: Cada agregado possui seu repositório com interface no Domain. Transações são controladas via `IUnitOfWork.CommitAsync()`.

### Motor de Compra
- A compra é consolidada na **conta master** para depois distribuir, evitando múltiplas ordens pequenas na B3.
- **Lote padrão** (múltiplos de 100) é priorizado; resíduo vai para o **mercado fracionário** (sufixo F).
- Distribuição usa `Math.Truncate` (sem arredondamento para cima) — resíduos ficam na custódia master para a próxima execução.
- Dias de compra (5, 15, 25) e demais constantes financeiras estão centralizadas em `RegrasFinanceiras.cs` para facilitar manutenção.

### Cotações (COTAHIST)
- O parser lê arquivos posicionais da B3 (245 caracteres/linha, encoding ISO-8859-1).
- Suporta ambos os formatos de nome: `COTAHIST_DDDMMYYYY.TXT` (formato real da B3) e `COTAHIST_DYYYYMMDD.TXT`.
- Arquivos são ordenados por data decrescente para priorizar a cotação mais recente.

### Qualidade de Código
- **SOLID**: Princípio de Responsabilidade Única (ex: `CarteiraService` separado de `ClienteService`), métodos grandes decompostos em métodos privados focados.
- **Magic numbers** eliminados via classe `RegrasFinanceiras` (constantes como `ParcelasPorMes`, `TaxaIRDedoDuro`, `LimiteIsencaoIR`, `AliquotaIRVenda`).
- **Exception handling**: Catches específicos com `ILogger` em vez de catches genéricos silenciosos.
- **Middleware centralizado** para tratamento de exceções (mapeia `DomainException` para HTTP 400/404/409).

### CQRS com MediatR
- Separação clara entre **Commands** (escrita: adesão, saída, compra, rebalanceamento) e **Queries** (leitura: carteira, rentabilidade, cesta, custódia).
- Controllers recebem `IMediator` em vez de injetar serviços diretamente — o pipeline MediatR despacha para o Handler correto.
- **LoggingBehavior** como `IPipelineBehavior<TRequest, TResponse>`: intercepta todos os Commands/Queries, loga entrada/saída com tempo de execução via Stopwatch. Demonstra cross-cutting concerns sem tocar nos handlers.
- Handlers delegam para os serviços existentes (Application layer), mantendo a lógica de negócio isolada e testável.

```
Application/CQRS/
├── Commands/   (6)   AderirCommand, SairCommand, CadastrarCestaCommand, ...
├── Queries/    (5)   ConsultarCarteiraQuery, ObterCestaAtualQuery, ...
├── Handlers/   (11)  Um handler por Command/Query
└── Behaviors/  (1)   LoggingBehavior (cross-cutting)
```

### Observabilidade (Serilog)
- **Serilog.AspNetCore** com output JSON estruturado no console.
- **CorrelationId** propagado via `LogContext.PushProperty` no `ExceptionMiddleware` — cada log produzido durante uma request carrega o mesmo `X-Request-Id` do response header.
- `UseSerilogRequestLogging()` gera log automático de cada request HTTP com status code e duração.
- Bootstrap logger para capturar erros durante a inicialização da aplicação.

### Frontend (React SPA)
- **React 18 + TypeScript + Vite**: SPA moderna com tipagem forte e build rápido.
- **Interfaces TypeScript** espelham exatamente os DTOs C# do backend, garantindo type safety end-to-end.
- **Vite dev server** com proxy para a API (`/api` → `localhost:5000`), eliminando problemas de CORS em desenvolvimento.
- **6 páginas** cobrindo todos os endpoints: Adesão, Carteira, Rentabilidade, Cesta Top Five (com abas atual/nova/histórico), Custódia Master, Motor de Compra & Rebalanceamento.
- **Layout responsivo** com sidebar de navegação organizada por contexto (Cliente, Administração, Motor).

### CI/CD (GitHub Actions)
- Pipeline `.github/workflows/ci.yml` que executa em push/PR para main/develop.
- Steps: checkout → setup .NET 8 → restore → build → testes unitários → testes de integração.
- Testes de integração usam banco InMemory (sem dependência de Docker no CI).
