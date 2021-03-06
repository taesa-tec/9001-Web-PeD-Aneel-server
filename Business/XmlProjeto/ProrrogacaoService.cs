﻿using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using APIGestor.Data;
using APIGestor.Models;
using Microsoft.AspNetCore.Identity;
using APIGestor.Security;
using System.Xml.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Xml;
using System.Text;
using Microsoft.AspNetCore.Hosting;

namespace APIGestor.Business
{
    public class XmlProrrogacaoService : IXmlService<ProrrogacaoProjeto>
    {
        private GestorDbContext _context;
        public XmlProrrogacaoService(GestorDbContext context)
        {
            _context = context;
        }
        public Resultado ValidaXml(int ProjetoId)
        {
            Projeto projeto = _context.Projetos
                        .Include("CatalogEmpresa")
                        .Include("Etapas")
                        .Where(p => p.Id == ProjetoId)
                        .FirstOrDefault();

            var resultado = new Resultado();
            resultado.Acao = "Validação de dados";
            if (projeto.Codigo == null)
                resultado.Inconsistencias.Add("Código do Projeto não gerado");
            int Duracao = projeto.Etapas.Sum(p => p.Duracao);
            if ((projeto.TipoValor == "PD" && Duracao > 60) || (projeto.TipoValor == "PG" && Duracao > 12))
                resultado.Inconsistencias.Add("Duração máxima execedida para o projeto");
            return resultado;
        }
        public ProrrogacaoProjeto GerarXml(int ProjetoId, string Versao, string UserId)
        {
            ProrrogacaoProjeto Prorrogacao = new ProrrogacaoProjeto();
            Projeto projeto = _context.Projetos
                        .Include("CatalogEmpresa")
                        .Include("Etapas")
                        .Where(p => p.Id == ProjetoId)
                        .FirstOrDefault();
            Prorrogacao.PD_PrazoExecProjeto = new PD_PrazoExecProjeto
            {
                Projeto = new ProProjeto
                {
                    CodProjeto = projeto.Codigo,
                    Duracao = projeto.Etapas.Sum(p => p.Duracao)
                }
            };
            return Prorrogacao;
        }
    }
}
