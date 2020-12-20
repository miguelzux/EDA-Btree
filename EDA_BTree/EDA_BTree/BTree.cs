using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EDA_BTree
{
    class BTree
    {

        private Canvas canvas;
        public Pagina raiz;
        static private int d;

        static public int D { get { return d; } }

        public BTree(Canvas _canvas, int D = 2)
        {
            canvas = _canvas;
            raiz = new Pagina();
            canvas.Children.Add(raiz);
            d = D;
        }

        public async Task<string> Buscar(int value)
        {
            Tuple<Pagina,Nodo> pin = await raiz.Buscar(value);
            Nodo nodo = pin.Item2;
            if (nodo != null)
                return string.Format("Valor {0} encontrado", value);
            else
                return string.Format("Valor {0} no encontrado", value);
        }

        public async Task<string> Insertar(int value)
        {
            Tuple<Pagina,Nodo> pin = await raiz.Buscar(value);
            Nodo nodo = pin.Item2; Pagina pagina = pin.Item1;
            if (nodo != null)
                return string.Format("Valor {0} ya existe", value);
            else
            {
                await pagina.Insertar(value);
                if (raiz.PaginaPadre != null)
                    raiz = raiz.PaginaPadre;
                await raiz.reajustarPocision(true);
                return string.Format("Valor {0} insertado", value);
            }
        }

        public async Task<string> Eliminar(int value)
        {
            Tuple<Pagina, Nodo> pin = await raiz.Buscar(value);
            Nodo nodo = pin.Item2; 
            if (nodo == null)
                return string.Format("Valor {0} no existe", value);
            else
            {
                await Pagina.eliminar(nodo);
                if(raiz.nodos.Count == 0)
                {
                    if (raiz.hijos.Count > 0 && raiz.hijos[0].pagina != null)
                        raiz = raiz.hijos[0].pagina;
                }
                await raiz.reajustarPocision(true);
                return string.Format("Valor {0} eliminado", value);
            }
        }
    }
}
