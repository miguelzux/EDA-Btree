using System.Windows.Shapes;

namespace EDA_BTree
{
    public class PaginaHijo
    {
        public Puntero puntero = null;
        public Line arista = null;
        public Pagina pagina = null;
        public Pagina paginaPadre = null;

        public PaginaHijo(Pagina parent)
        {
            paginaPadre = parent;
        }

        public Pagina PaginaPadre
        {
            get { return paginaPadre; }
            set
            {
                paginaPadre = value;
                puntero.PaginaPadre = value;
            }
        }
    }
}
