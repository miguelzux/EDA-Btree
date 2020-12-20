using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace EDA_BTree
{
    public class Pagina : Canvas
    {
        public static readonly double PageHeight = 75;
        public static readonly double PageSpaceWidth = 30;
        public List<Nodo> nodos;
        public List<PaginaHijo> hijos;
        public PaginaHijo link = null;

        public bool existeNodoPocision
        {
            get
            {
                bool ok = Canvas.GetLeft(this) == distanciaIzquierda && Canvas.GetTop(this) == distanciaArriba;
                foreach (var hijo in hijos)
                    if (hijo.pagina != null)
                        ok = ok && hijo.pagina.existeNodoPocision;
                return ok;
            }
        }

        public Pagina PaginaPadre
        {
            get
            {
                return link == null ? null : link.paginaPadre;
            }
        }

        public new int Height
        {
            get { return PaginaPadre == null ? 0 : PaginaPadre.Height + 1; }
        }

        public double distanciaArriba
        {
            get { return Height == 0 ? 0 : PageHeight; }
        }

        public int Indice
        {
            get { return link == null ? 0 : link.puntero.Indice; }
        }

        public double distanciaIzquierda
        {
            get
            {
                if (PaginaPadre == null)
                    return (((Canvas)Parent).ActualWidth - vAnchoPagina) / 2;
                var p = PaginaPadre;
                double maxC = (double)p.maxAnchoHijo;
                double x0 = (p.vAnchoPagina - p.vAnchoPaginaArbol) / 2 + Indice * (maxC + PageSpaceWidth) + (maxC - vAnchoPagina) / 2;
                return x0;
            }
        }

        public double? maxAnchoHijo
        {
            get
            {
                if (hijos.Count() == 0 || hijos[0].pagina == null)
                    return null;
                double max = 0;
                foreach (var c in hijos)
                    max = Math.Max(max, c.pagina.vAnchoPaginaArbol);
                return max;
            }
        }

        public double vAnchoPaginaArbol
        {
            get
            {
                var maxC = maxAnchoHijo;
                return maxC == null ? vAnchoPagina : (double)maxC * hijos.Count + PageSpaceWidth * (hijos.Count - 1);
            }
        }

        public double vAnchoPagina
        {
            get
            {
                return hijos.Count * Puntero.AnchoDefecto + nodos.Count * Nodo.AnchoDefecto;
            }
        }

        public Pagina(PaginaHijo _link = null)
        {
            Effect = Estilos.shadow;

            nodos = new List<Nodo>();
            hijos = new List<PaginaHijo>();
            link = _link;
        }

        public void redibujar()
        {
            ((Canvas)Parent).UpdateLayout();
            reajustarPocision();
        }

        private void actualizarJerarquia()
        {
            if (Parent == null)
            {
                PaginaPadre.Children.Add(this);
                var transform = this.hijos[0].puntero.TransformToVisual(PaginaPadre);
                var pocision = transform.Transform(new Point(0, 0));
                Canvas.SetTop(this, pocision.X);
                Canvas.SetLeft(this, pocision.Y);
            }
            foreach (var c in hijos)
            {
                Pagina pagina = c.pagina;
                if (pagina != null && pagina.Parent != this)
                {
                    if (pagina.PaginaPadre != this) throw new Exception();
                    var transform = pagina.hijos[0].puntero.TransformToVisual(this);
                    var posicion = transform.Transform(new Point(0, 0));
                    Canvas.SetLeft(pagina, posicion.X);
                    Canvas.SetTop(pagina, posicion.Y);
                    if (pagina.Parent != null)
                        ((Pagina)pagina.Parent).Children.Remove(pagina);
                    this.Children.Add(pagina);
                }
                ((Canvas)Parent).UpdateLayout();
                if (pagina != null)
                    pagina.actualizarJerarquia();
                UpdateLayout();
            }
        }

        public Task reajustarPocision(bool animar = false)
        {
            var tcs = new TaskCompletionSource<object>();

            if (existeNodoPocision)
            {
                tcs.SetResult(null);
                return tcs.Task;
            }

            Canvas p = (Canvas)Parent;

            if (hijos.Count() > 0)
            {
                if (!animar)
                {
                    Canvas.SetTop(this, distanciaArriba);
                    Canvas.SetLeft(this, distanciaIzquierda);
                    var tarea = new List<Task>();
                    foreach (var c in hijos)
                        if (c.pagina != null)
                            tarea.Add(c.pagina.reajustarPocision());
                    Task.WhenAll(tarea).GetAwaiter().OnCompleted(() => tcs.SetResult(null));
                }
                else
                {
                    var tarea = new List<Task>();
                    foreach (var c in hijos)
                        if (c.pagina != null)
                            tarea.Add(c.pagina.reajustarPocision(true));

                    var animacionArriba = Animaciones.slideAnimacionF(distanciaArriba);
                    var animacionIzquierda = Animaciones.slideAnimacionF(distanciaIzquierda);

                    if (double.IsNaN(Canvas.GetTop(this)))
                        animacionArriba.From = distanciaArriba;
                    if (double.IsNaN(Canvas.GetLeft(this)))
                        animacionIzquierda.From = distanciaIzquierda;

                    animacionArriba.FillBehavior = FillBehavior.Stop;
                    animacionIzquierda.FillBehavior = FillBehavior.Stop;
                    animacionArriba.Completed += (s, a) =>
                    {
                        tcs.SetResult(null);
                        Canvas.SetTop(this, distanciaArriba);
                    };
                    animacionIzquierda.Completed += (s, a) => Canvas.SetLeft(this, distanciaIzquierda);
                    BeginAnimation(Canvas.TopProperty, animacionArriba);
                    BeginAnimation(Canvas.LeftProperty, animacionIzquierda);
                }
            }
            else tcs.SetResult(null);
            return tcs.Task;
        }

        public async Task actualizarElementosVista(bool animar = false)
        {
            List<Task> tarea = new List<Task>();
            foreach (var nodo in nodos)
                tarea.Add(nodo.actualizar(true));
            foreach (var hijo in hijos)
                tarea.Add(hijo.puntero.actualizar(true));
            await Task.WhenAll(tarea);
        }


        public async Task<Tuple<Pagina, Nodo>> Buscar(int valor)
        {
            if (nodos.Count() == 0)
                return new Tuple<Pagina, Nodo>(this, null);

            int i;

            for (i = 0; i < nodos.Count(); i++)
            {
                await Estilos.highlight(nodos[i]);
                if (nodos[i].Value >= valor)
                    break;
            }

            if (i < nodos.Count() && nodos[i].Value == valor)
                return new Tuple<Pagina, Nodo>(this, nodos[i]);

            if (hijos[i].pagina != null)
            {
                await Estilos.highlight(hijos[i].puntero);
                Pagina hijo = hijos[i].pagina;
                return await hijo.Buscar(valor);
            }
            else
                return new Tuple<Pagina, Nodo>(this, null);
        }

        public async Task Insertar(int valor)
        {
            var nodo = new Nodo(valor);
            Estilos.SetStyle(nodo, "Insert");

            await Insertar_Nodo(nodo);
            await dividir();

            await Estilos.highlight(nodo, "Insert");
        }

        public async Task Insertar_Nodo(Nodo nodo, PaginaHijo previo = null, PaginaHijo siguiente = null)
        {
            int valor = nodo.Value;

            Puntero pizquierdo, pderecho;

            if (previo == null)
            {
                pizquierdo = new Puntero(0);
                pderecho = new Puntero(1);
                previo = new PaginaHijo(this) { puntero = pizquierdo };
                siguiente = new PaginaHijo(this) { puntero = pderecho };
            }
            else
            {
                pizquierdo = previo.puntero;
                pderecho = siguiente.puntero;
            }

            previo.paginaPadre = siguiente.paginaPadre = nodo.PaginaPadre = pizquierdo.PaginaPadre = pderecho.PaginaPadre = this;

            if (nodos.Count() == 0)
            {
                foreach (var hijo in hijos)
                    Children.Remove(hijo.puntero);
                hijos.Clear();

                UpdateLayout();

                nodo.Indice = 0;
                pizquierdo.Indice = 0;
                pderecho.Indice = 1;

                nodos.Add(nodo);
                hijos.Add(previo);
                hijos.Add(siguiente);

                await Task.WhenAll(reajustarPocision(true), actualizarElementosVista());
            }
            else
            {
                int i = 0;
                for (i = 0; i < nodos.Count(); i++)
                    if (nodos[i].Value >= valor)
                        break;
                nodo.Indice = i;
                pizquierdo.Indice = i;
                pderecho.Indice = i + 1;

                if (i < nodos.Count())
                {
                    if (nodos[i].Value == valor)
                        return;

                    Children.Remove(hijos[i].puntero);
                    hijos[i] = siguiente;
                    pderecho.Indice--;
                    await Task.WhenAll(pizquierdo.actualizar(), pderecho.actualizar());

                    List<Task> tarea = new List<Task>();

                    for (int j = i; j < nodos.Count(); j++)
                    {
                        tarea.Add(nodos[j].incrementoProgreso(1));
                        tarea.Add(hijos[j].puntero.incrementoProgreso(1));
                    }
                    tarea.Add(hijos.Last().puntero.incrementoProgreso(1));
                    hijos.Insert(i, previo);

                    nodos.Insert(i, nodo);
                    nodo.Indice = i;

                    tarea.Add(reajustarPocision(true));
                    tarea.Add(actualizarElementosVista());

                    await Task.WhenAll(tarea);
                }
                else
                {
                    Children.Remove(hijos[i].puntero);
                    hijos[i] = previo;

                    hijos.Insert(i + 1, siguiente);

                    nodos.Insert(i, nodo);
                    nodo.Indice = i;

                    actualizarJerarquia();
                    await Task.WhenAll(reajustarPocision(true), actualizarElementosVista());
                }
            }
        }

        public async Task dividir()
        {

            if (nodos.Count() <= BTree.D) return;

            Pagina iPagina = PaginaPadre == null ? this : PaginaPadre;
            PaginaHijo lHijo = new PaginaHijo(iPagina), rHijo = new PaginaHijo(iPagina);
            Pagina lPagina = new Pagina(lHijo), rPagina = new Pagina(rHijo);

            int mitad = nodos.Count / 2;
            Nodo mitadNodo = nodos[mitad];
            Puntero lPointer = new Puntero(mitad) { PaginaPadre = iPagina }, rPointer = new Puntero(mitad + 1) { PaginaPadre = iPagina };
            await lPointer.actualizar(); await rPointer.actualizar();

            for (int i = 0; i < mitad; i++)
            {
                lPagina.nodos.Add(nodos[i]);
                nodos[i].PaginaPadre = lPagina;

                lPagina.hijos.Add(hijos[i]);
                hijos[i].paginaPadre = lPagina;
                hijos[i].puntero.PaginaPadre = lPagina;
            }
            lPagina.hijos.Add(hijos[mitad]);
            hijos[mitad].paginaPadre = lPagina;
            hijos[mitad].puntero.PaginaPadre = lPagina;

            for (int i = mitad + 1; i < nodos.Count; i++)
            {
                rPagina.nodos.Add(nodos[i]);
                nodos[i].PaginaPadre = rPagina;

                rPagina.hijos.Add(hijos[i]);
                hijos[i].paginaPadre = rPagina;
                hijos[i].puntero.PaginaPadre = rPagina;

                nodos[i].Indice = i - mitad - 1;
                hijos[i].puntero.Indice = i - mitad - 1;
            }
            rPagina.hijos.Add(hijos.Last());
            hijos.Last().paginaPadre = rPagina;
            hijos.Last().puntero.PaginaPadre = rPagina;
            hijos.Last().puntero.Indice = hijos.Count - mitad - 2;

            lHijo.pagina = lPagina;
            rHijo.pagina = rPagina;

            rHijo.pagina.MouseDown += (s, e) => { };

            lHijo.puntero = lPointer;
            rHijo.puntero = rPointer;

            nodos.Clear();
            hijos.Clear();

            if (PaginaPadre == null)
            {
                mitadNodo.Indice = 0;
                iPagina.nodos.Add(mitadNodo);
                lPointer.Indice = 0;
                rPointer.Indice = 1;
                iPagina.hijos.Add(lHijo);
                iPagina.hijos.Add(rHijo);

                iPagina.actualizarJerarquia();
                await Task.WhenAll(iPagina.actualizarElementosVista(true),
                                   lPagina.actualizarElementosVista(true),
                                   rPagina.actualizarElementosVista(true),
                                   iPagina.reajustarPocision(true));
            }
            else
            {
                await iPagina.Insertar_Nodo(mitadNodo, lHijo, rHijo);
                iPagina.UpdateLayout();
                iPagina.actualizarJerarquia();
                await Task.WhenAll(iPagina.reajustarPocision(true),
                                   lPagina.actualizarElementosVista(true),
                                   rPagina.actualizarElementosVista(true));

                if (PaginaPadre != null)
                    ((Canvas)Parent).Children.Remove(this);
            }
            await iPagina.dividir();
        }

        static public async Task eliminar(Nodo nodo, int? _index = null, bool inline = false)
        {
            Panel.SetZIndex(nodo, -1);

            Pagina pagina = nodo.PaginaPadre;
            var tarea = new List<Task>();

            int indice = _index == null ? nodo.Indice : (int)_index;

            if (pagina.hijos[0].pagina == null || inline)
            {
                var rHijo = pagina.hijos[indice + 1];
                bool bSlideAnimacion = pagina.nodos.Count != 1;
                for (int i = indice + 1; i < pagina.nodos.Count; i++)
                {
                    tarea.Add(pagina.nodos[i].incrementoProgreso(-1, bSlideAnimacion));
                    tarea.Add(pagina.hijos[i].puntero.incrementoProgreso(-1, bSlideAnimacion));
                }
                tarea.Add(pagina.hijos.Last().puntero.incrementoProgreso(-1, bSlideAnimacion));

                await Task.WhenAll(tarea);

                pagina.nodos.Remove(nodo);
                pagina.Children.Remove(nodo);

                pagina.Children.Remove(rHijo.puntero);
                pagina.hijos.Remove(rHijo);
                if (!inline)
                    await pagina.catenation();
            }
            else 
            {
                Nodo sNodo = buscarSiguiente(pagina, indice);

                if (sNodo == null)
                    throw new Exception();

                Pagina sPage = sNodo.PaginaPadre;

                tarea.Add(eliminar(sNodo, 0, true));

                pagina.nodos[indice] = sNodo;
                sNodo.PaginaPadre = pagina;
                sNodo.Indice = indice;

                tarea.Add(sNodo.actualizar(true));
                await Task.WhenAll(tarea);

                pagina.Children.Remove(nodo);
                if (!inline)
                    await sPage.catenation();
            }
        }

        private async Task catenation()
        {
            if (nodos.Count >= BTree.D)
                return;

            if (PaginaPadre == null)
            {
                if (nodos.Count > 0)
                    return;

                var parent = (Canvas)Parent;

                if (hijos.Count == 0 || hijos[0].pagina == null)
                {
                    Children.Clear();
                    hijos.Clear();
                    return;
                }

                var nRoot = hijos[0].pagina;
                var pocision = nRoot.TransformToVisual(parent).Transform(new Point(0, 0));
                Canvas.SetLeft(nRoot, pocision.X);
                Canvas.SetTop(nRoot, pocision.Y);
                parent.Children.Remove(this);
                this.Children.Remove(nRoot);
                nRoot.link = null;
                parent.Children.Add(nRoot);

                return;
            }

            int lIndex = link.puntero.Indice, rIndex = lIndex + 1;
            if (rIndex == PaginaPadre.hijos.Count)
            {
                rIndex = lIndex;
                lIndex = rIndex - 1;
            }

            PaginaHijo lHijo = PaginaPadre.hijos[lIndex], rHijo = PaginaPadre.hijos[rIndex];
            Nodo mitadNodo = PaginaPadre.nodos[lIndex];

            var tarea = new List<Task>();
            tarea.Add(eliminar(mitadNodo, mitadNodo.Indice, true));

            mitadNodo.Indice = lHijo.pagina.nodos.Count;
            mitadNodo.PaginaPadre = lHijo.pagina;
            lHijo.pagina.nodos.Add(mitadNodo);

            foreach (var nodo in rHijo.pagina.nodos)
            {
                nodo.Indice += lHijo.pagina.nodos.Count;
                nodo.PaginaPadre = lHijo.pagina;
            }

            foreach (var hijo in rHijo.pagina.hijos)
            {
                hijo.puntero.Indice += lHijo.pagina.hijos.Count;
                hijo.PaginaPadre = lHijo.pagina;
            }

            lHijo.pagina.nodos.AddRange(rHijo.pagina.nodos);
            lHijo.pagina.hijos.AddRange(rHijo.pagina.hijos);

            rHijo.pagina.nodos.Clear();
            rHijo.pagina.hijos.Clear();

            PaginaPadre.actualizarJerarquia();
            tarea.Add(lHijo.pagina.actualizarElementosVista(true));
            await Task.WhenAll(tarea);

            PaginaPadre.hijos.Remove(rHijo);
            PaginaPadre.Children.Remove(rHijo.pagina);

            await lHijo.pagina.dividir();
            await PaginaPadre.catenation();
        }

        private static Nodo buscarSiguiente(Pagina pagina, int indice)
        {
            pagina = pagina.hijos[indice + 1].pagina;
            while (pagina.hijos[0].pagina != null)
                pagina = pagina.hijos[0].pagina;
            return pagina.nodos[0];
        }
    }
}
