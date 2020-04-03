using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using APIGestor.Models.Demandas;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using APIGestor.Dtos;
using APIGestor.Exceptions.Demandas;
using APIGestor.Services;
using APIGestor.Services.Demandas;
using AutoMapper;
using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using HtmlAgilityPack;

namespace APIGestor.Controllers.Demandas
{
    public partial class DemandaController
    {
        private const string patt = "(<[\\w|\\d]+(?:\\b[^>]*)?>\\s*|\\s*</[\\w|\\d]+>\\s*)";

        [HttpPost("Criar")]
        public ActionResult<Demanda> CriarDemanda([FromBody] string titulo)
        {
            return Service.CriarDemanda(titulo, this.userId());
        }

        [HttpHead("{id:int}/Access")]
        public ActionResult HasAccess(int id)
        {
            if (Service.DemandaExist(id))
            {
                if (Service.UserCanAccess(id, this.userId()))
                    return Ok();
                else
                    return Forbid();
            }

            return NotFound();
        }

        [HttpGet("{id:int}")]
        public ActionResult<Demanda> GetById(int id)
        {
            if (Service.UserCanAccess(id, this.userId()))
                return Service.GetById(id);
            return NotFound();
        }

        [HttpPut("{id}/Captacao")]
        public ActionResult EnviarCaptacao(int id)
        {
            Service.EnviarCaptacao(id, this.userId());
            return Ok();
        }

        [HttpPut("{id}/EquipeValidacao")]
        public ActionResult SetEquipeValidacao(int id, [FromBody] JObject data)
        {
            var superiorDireto = data.Value<string>("superiorDireto");
            if (String.IsNullOrWhiteSpace(superiorDireto))
            {
                return BadRequest("Superior Direto não informado!");
            }

            if (!Service.DemandaExist(id))
            {
                return NotFound();
            }

            Service.SetSuperiorDireto(id, superiorDireto);

            return Ok();
        }

        [HttpGet("{id}/EquipeValidacao")]
        public ActionResult<object> GetEquipeValidacao(int id)
        {
            return new
            {
                superiorDireto = Service.GetSuperiorDireto(id)
            };
        }

        [HttpPut("{id}/Revisor")]
        public ActionResult<Demanda> SetRevisor(int id, [FromBody] JObject data)
        {
            var revisorId = data.Value<string>("revisorId");
            if (String.IsNullOrWhiteSpace(revisorId))
            {
                return BadRequest("Rivisor não informado!");
            }

            if (!Service.DemandaExist(id))
            {
                return NotFound();
            }

            if (sistemaService.GetEquipePeD().Coordenador == this.userId())
            {
                try
                {
                    Service.ProximaEtapa(id, this.userId(), revisorId);
                }
                catch (DemandaException exception)
                {
                    return BadRequest(exception);
                }
                catch (System.Exception)
                {
                    throw;
                }

                return GetById(id);
            }

            return Forbid();
        }

        [HttpPut("{id}/ProximaEtapa")]
        public ActionResult<Demanda> AlterarStatusDemanda(int id, [FromBody] JObject data)
        {
            var comentario = data.Value<string>("comentario");
            Service.ProximaEtapa(id, this.userId());

            if (!String.IsNullOrWhiteSpace(comentario))
            {
                Service.AddComentario(id, comentario, this.userId());
            }

            return GetById(id);
        }

        [HttpPut("{id}/Etapa")]
        public ActionResult<Demanda> SetEtapa(int id, [FromBody] JObject data)
        {
            var etapa = (DemandaEtapa) data.Value<int>("status");
            if (etapa < DemandaEtapa.Captacao)
            {
                Service.SetEtapa(id, etapa, this.userId());
            }
            else
            {
                Service.EnviarCaptacao(id, this.userId());
            }


            return GetById(id);
        }

        [HttpPut("{id:int}/Reiniciar")]
        public ActionResult<Demanda> Reiniciar(int id, [FromBody] JObject data)
        {
            if (!Service.DemandaExist(id))
                return NotFound();

            var motivo = data.Value<string>("motivo");

            if (string.IsNullOrWhiteSpace(motivo))
            {
                motivo = "Motivo não informado";
            }

            Service.ReprovarReiniciar(id, this.userId());
            Service.AddComentario(id, motivo, this.userId());


            return GetById(id);
        }

        [HttpPut("{id:int}/ReprovarPermanente")]
        public ActionResult<Demanda> Finalizar(int id, [FromBody] JObject data)
        {
            if (!Service.DemandaExist(id))
                return NotFound();

            var motivo = data.Value<string>("motivo");


            Service.ReprovarPermanente(id, this.userId());
            Service.AddComentario(id, motivo, this.userId());

            return CreatedAtAction(nameof(GetById), new {id});
        }

        [HttpGet("{id:int}/File/")]
        public ActionResult<object> GetDemandaFiles(int id)
        {
            return Service.GetDemandaFiles(id);
        }

        [HttpGet("{id:int}/File/{file_id:int}")]
        public ActionResult<object> GetDemandaFile(int id, int file_id)
        {
            var file = Service.GetDemandaFile(id, file_id);
            if (file != null && System.IO.File.Exists(file.Path))
            {
                return PhysicalFile(file.Path, file.ContentType, file.Name);
            }

            return NotFound();
        }

        [HttpGet("{id:int}/Form/{form}")]
        public ActionResult<object> GetDemandaFormValue(int id, string form)
        {
            var data = Service.GetDemandaFormData(id, form);
            if (data != null)
            {
                return data;
            }

            return default(object);
        }

        [HttpPut("{id}/Form/{form}")]
        public ActionResult<object> SalvarDemandaFormValue(int id, string form, [FromBody] JObject data)
        {
            if (Service.DemandaExist(id))
            {
                Service.SalvarDemandaFormData(id, form, data).RunSynchronously();
                var formName = DemandaService.GetForm(form).Title;
                Service.LogService.Incluir(this.userId(), id,
                    String.Format("Atualizou Dados do formulário {0}", formName), data, "demanda-form");
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{id:int}/Form/{form}/Pdf")]
        public ActionResult<object> GetDemandaPDF(int id, string form)
        {
            var filename = Service.GetDemandaFormPdfFilename(id, form);
            if (System.IO.File.Exists(filename))
            {
                var name = String.Format("demanda-{0}-{1}.pdf", id, form);
                var response = PhysicalFile(filename, "application/pdf", name);
                if (Request.Query["dl"] == "1")
                {
                    response.FileDownloadName = name;
                }

                return response;
            }
            else
            {
                return NotFound(filename);
            }
        }

        [AllowAnonymous]
        [HttpGet("{id:int}/Form/{form}/History")]
        public ActionResult GetDemandaHistorico(int id, string form, [FromServices] IMapper mapper)
        {
            var historico =
                mapper.Map<List<DemandaFormHistoricoListItemDto>>(Service.GetDemandaFormHistoricos(id, form));
            return Ok(historico);
        }

        [AllowAnonymous]
        [HttpGet("{id:int}/Form/{form}/Diff/{historyId}")]
        public async Task<ActionResult> GetDemandaHistoricoDiff(int id, string form, int historyId,
            [FromServices] IViewRenderService viewRenderService)
        {
            var diffBuilder = new InlineDiffBuilder(new Differ());
            var demandaForm = Service.GetDemandaFormData(id, form);
            var historico = Service.GetDemandaFormHistorico(historyId);
            var htmlOld = new HtmlDocument();
            var htmlNew = new HtmlDocument();
            IChunker chunker;

            chunker = new CustomFunctionChunker(s =>
                Regex.Split(s, "(<[\\w|\\d]+(?:\\b[^>]*)?>\\s*|\\s*</[\\w|\\d]+>\\s*)"));

            if (historico == null)
                return NotFound(new
                {
                    revisaoAtual = 0,
                    html = ""
                });

            htmlNew.LoadHtml(demandaForm.Html);
            htmlOld.LoadHtml(historico.Content);
            var bodyNew = htmlNew.DocumentNode.SelectSingleNode("//body");
            var bodyOld = htmlOld.DocumentNode.SelectSingleNode("//body");

            var diffFrom = diffBuilder.BuildDiffModel(
                bodyOld.InnerHtml, //HttpUtility.HtmlDecode(htmlOld.DocumentNode.InnerText),
                bodyNew.InnerHtml,
                true,
                true,
                chunker
            );
            var from = await viewRenderService.RenderToStringAsync("Pdf/Diff", diffFrom);

            return Ok(new
            {
                revisaoAtual = demandaForm.Revisao,
                lastUpdate = demandaForm.LastUpdate,
                html = from, //bodyNew.InnerHtml,
            });
        }

        [AllowAnonymous]
        [HttpGet("{id:int}/Form/{form}/Debug")]
        public async Task<ActionResult<object>> GetDemandaTeste(int id, string form)
        {
            var doc = await Service.DemandaFormHtml(Service.GetDemandaFormView(id, form));
            if (doc != null)
            {
                return Content(doc, "text/html");
            }

            return NotFound();
        }

        [AllowAnonymous]
        [HttpGet("{id:int}/Form/{form}/DiffPlex/{version}")]
        public ActionResult TestDiffPlex(int id, string form, int version, [FromServices] IMapper mapper,
            [FromServices] IViewRenderService viewRenderService)
        {
            var diffBuilder = new InlineDiffBuilder(new Differ());
            var demandaForm = Service.GetDemandaFormData(id, form);
            var historico = mapper.Map<List<DemandaFormHistoricoDto>>(Service.GetDemandaFormHistoricos(id, form));
            var html = new HtmlDocument();
            var htmlNew = new HtmlDocument();
            IChunker chunker;

            chunker = new CustomFunctionChunker(s =>
                Regex.Split(s, "(<[\\w|\\d]+(?:\\b[^>]*)?>\\s*|\\s*</[\\w|\\d]+>\\s*)"));
            //chunker = new CustomFunctionChunker(s => Regex.Split(s, "(?=(?:<[\\w|\\d]+\\b[^>]*>|</[\\w|\\d]+>))"));
            // chunker = new CustomFunctionChunker(s => Regex.Split(s, "(?=[\\.;!\\?]|\\n{2,})\\s*?"));
            //chunker = new LineChunker();
            //chunker = new DelimiterChunker(new[] {'.', ';', '!', '?'});


            if (historico == null || historico.Count < version) return Ok();

            htmlNew.LoadHtml(demandaForm.Html);
            html.LoadHtml(historico.ElementAt(version).Content);
            var bodyNew = htmlNew.DocumentNode.SelectSingleNode("//body");
            var bodyOld = html.DocumentNode.SelectSingleNode("//body");

            var diff = diffBuilder.BuildDiffModel(
                bodyOld.InnerHtml, //HttpUtility.HtmlDecode(htmlOld.DocumentNode.InnerText),
                bodyNew.InnerHtml,
                true,
                true,
                chunker
            ); // HttpUtility.HtmlDecode(htmlNew.DocumentNode.InnerText));
            //var content = await viewRenderService.RenderToStringAsync("Pdf/Diff", diff);
            return Ok(diff);
            //return Content(content, "text/html");
        }


        [HttpGet("{id:int}/Logs")]
        public ActionResult<List<DemandaLog>> GetDemandaLog(int id)
        {
            if (Service.UserCanAccess(id, this.userId()))
            {
                return Service.GetDemandaLogs(id);
            }

            return Forbid();
        }
    }
}