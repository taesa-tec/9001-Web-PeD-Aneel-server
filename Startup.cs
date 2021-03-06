﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using APIGestor.Data;
using APIGestor.Models;
using APIGestor.Security;
using APIGestor.Business;
using Swashbuckle.AspNetCore.Swagger;
using System.Globalization;
using APIGestor.Authorizations;
using Microsoft.AspNetCore.Authorization;

namespace APIGestor
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Configurando o acesso a dados de projetos
            if (System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Stage")
            {
                var connectionString = System.Environment.GetEnvironmentVariable("ConnectionString");
                services.AddDbContext<GestorDbContext>(options =>
                    options.UseSqlServer(connectionString));
            }
            else
            {
                var connectionString = Configuration.GetConnectionString("BaseGestor");
                services.AddDbContext<GestorDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("BaseGestor")));
            }

            services.AddScoped<CatalogService>();
            services.AddScoped<MailService>();
            services.AddScoped<UserService>();
            services.AddScoped<ProjetoService>();
            services.AddScoped<UserProjetoService>();
            services.AddScoped<TemaService>();
            services.AddScoped<EmpresaService>();
            services.AddScoped<RecursoHumanoService>();
            services.AddScoped<AlocacaoRhService>();
            services.AddScoped<RecursoMaterialService>();
            services.AddScoped<AlocacaoRmService>();
            services.AddScoped<ProdutoService>();
            services.AddScoped<EtapaService>();
            services.AddScoped<LogService>();
            services.AddScoped<UploadService>();
            services.AddScoped<RelatorioEmpresaService>();
            services.AddScoped<RelatorioEtapaService>();
            services.AddScoped<RelatorioAtividadeService>();

            // Gerador Xml Services
            services.AddScoped<GeradorXmlService>();
            services.AddScoped<XmlProjetoPedService>();
            services.AddScoped<XmlInteressePedService>();
            services.AddScoped<XmlInicioExecService>();
            services.AddScoped<XmlProrrogacaoService>();
            services.AddScoped<XmlRelatorioFinalService>();
            services.AddScoped<XmlRelatorioAuditoriaService>();
            services.AddScoped<XmlProjetoGestaoService>();
            services.AddScoped<XmlRelatorioFinalGestaoService>();
            services.AddScoped<XmlRelatorioAuditoriaGestaoService>();
            ////////////////////////

            services.AddScoped<RegistroFinanceiroService>();

            services.AddScoped<RelatorioFinalService>();
            services.AddScoped<ResultadoCapacitacaoService>();
            services.AddScoped<ResultadoProducaoService>();
            services.AddScoped<ResultadoInfraService>();
            services.AddScoped<ResultadoIntelectualService>();
            services.AddScoped<ResultadoSocioAmbientalService>();
            services.AddScoped<ResultadoEconomicoService>();

            // Projeto Gestão
            services.AddScoped<AtividadeGestaoService>();

            // Ativando a utilização do ASP.NET Identity, a fim de
            // permitir a recuperação de seus objetos via injeção de
            // dependências
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+/";
            })
                .AddEntityFrameworkStores<GestorDbContext>()
                .AddDefaultTokenProviders()
                .AddErrorDescriber<PortugueseIdentityErrorDescriber>();

            // Configurando a dependência para a classe de validação
            // de credenciais e geração de tokens
            services.AddScoped<AccessManager>();

            var signingConfigurations = new SigningConfigurations();
            services.AddSingleton(signingConfigurations);

            var tokenConfigurations = new TokenConfigurations();
            new ConfigureFromConfigurationOptions<TokenConfigurations>(
                Configuration.GetSection("TokenConfigurations"))
                    .Configure(tokenConfigurations);
            services.AddSingleton(tokenConfigurations);

            services.AddSingleton<IAuthorizationHandler, ProjectAuthorizationHandler>();

            // Aciona a extensão que irá configurar o uso de
            // autenticação e autorização via tokens
            services.AddJwtSecurity(
                signingConfigurations, tokenConfigurations);

            services.AddCors();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddSwaggerGen(c =>
            {

                c.SwaggerDoc("v1",
                    new Info
                    {
                        Title = "Taesa - Gestor P&D",
                        Version = "v1",
                        Description = "API REST criada com o ASP.NET Core 2.1 para comunição com o Gestor P&D",
                        Contact = new Contact
                        {
                            Name = "Christiano de Chermont",
                            Url = "https://github.com/xerminada"
                        }
                    });
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
            GestorDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            MailService mailService)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            // Define Cultura Padrão
            var cultureInfo = new CultureInfo("pt-BR");
            cultureInfo.NumberFormat.CurrencySymbol = "R$";
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            // Criação de estruturas, usuários e permissões
            // na base do ASP.NET Identity Core (caso ainda não
            // existam)
            new IdentityInitializer(context, userManager, roleManager, mailService, Configuration)
                .Initialize();

            // Ativando middlewares para uso do Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
            });
            app.UseCors(builder => builder.AllowAnyMethod()
                                          .AllowAnyOrigin()
                                          .AllowAnyHeader()
                                          .AllowCredentials());
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}