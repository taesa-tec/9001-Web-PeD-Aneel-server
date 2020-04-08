namespace APIGestor.Models.Captacao
{
    public class CaptacaoArquivo : FileUpload
    {
        public bool AcessoFornecedor { get; set; }
        public int CaptacaoId { get; set; }
        public Captacao Captacao { get; set; }
    }
}