import { useEffect, useRef } from 'react';
import { Bell } from 'lucide-react';
import { Link, Outlet, useLocation, useMatches } from 'react-router';
import { useNoLeidas } from '../paginas/notificaciones/consultas';
import InterruptorTema from '../tema/InterruptorTema';
import BarraInferior from './BarraInferior';
import BarraLateral from './BarraLateral';
import { m } from 'motion/react';
import { useEntrada } from '../utilidades/movimiento';

// Cada sección entra subiendo y apareciendo; la clave reinicia la animación al cambiar de ruta.
function EntradaDePagina({ clave, children }) {
  const entrada = useEntrada();
  return (
    <m.div key={clave} {...entrada}>
      {children}
    </m.div>
  );
}

// Marco de las pantallas con sesión: barra lateral (escritorio) o inferior (móvil), encabezado con
// el título de la sección, avisos y tema, y el contenido. El título sale del «handle» de la ruta y
// es el único h1: las páginas dentro del marco empiezan en h2.
export default function Marco() {
  const coincidencias = useMatches();
  const titulo = [...coincidencias].reverse().find((c) => c.handle?.titulo)?.handle.titulo ?? '';

  const { pathname } = useLocation();
  const noLeidas = useNoLeidas();
  const contenido = useRef(null);
  const primeraVez = useRef(true);

  useEffect(() => {
    document.title = titulo ? `${titulo} · Rastreador de hábitos` : 'Rastreador de hábitos';
  }, [titulo]);

  // Al cambiar de sección, el foco pasa al contenido para que el lector de pantalla lo anuncie
  // (no en la primera carga, para no saltarse el encabezado).
  useEffect(() => {
    if (primeraVez.current) {
      primeraVez.current = false;
      return;
    }
    contenido.current?.focus({ preventScroll: true });
  }, [pathname]);

  return (
    <div className="relative min-h-dvh">
      <div className="fondo-ambiente pointer-events-none fixed inset-0 -z-10" aria-hidden="true" />
      <a
        href="#contenido"
        className="sr-only z-50 rounded-lg bg-acento px-4 py-2 text-sobre-acento focus:not-sr-only focus:fixed focus:top-3 focus:left-3"
      >
        Saltar al contenido
      </a>
      <BarraLateral />

      <div className="flex min-h-dvh flex-col md:pl-64">
        <header className="vidrio-fuerte sticky top-0 z-10 flex h-16 items-center gap-3 border-b border-vidrio-borde bg-vidrio px-4 md:px-8">
          <img src="/icono.svg" alt="" className="size-7 md:hidden" />
          <h1 className="flex-1 truncate text-xl font-semibold">{titulo}</h1>
          <Link
            to="/notificaciones"
            className="relative inline-flex size-10 items-center justify-center rounded-xl text-suave transition-colors hover:bg-superficie-suave/70 hover:text-texto"
            aria-label={noLeidas ? `Notificaciones, ${noLeidas} sin leer` : 'Notificaciones'}
            title="Notificaciones"
          >
            <Bell className="size-5" aria-hidden="true" />
            {noLeidas > 0 && (
              <span className="numeros absolute top-1 right-1 inline-flex min-w-4 items-center justify-center rounded-full bg-peligro px-1 text-[10px] leading-4 font-semibold text-white dark:text-[#2a0a12]" aria-hidden="true">
                {noLeidas > 9 ? '9+' : noLeidas}
              </span>
            )}
          </Link>
          <InterruptorTema />
        </header>

        <main id="contenido" ref={contenido} tabIndex={-1} aria-label={titulo || undefined} className="outline-none mx-auto w-full max-w-5xl flex-1 px-4 pt-6 pb-24 md:px-8 md:pb-10">
          <EntradaDePagina clave={pathname}>
            <Outlet />
          </EntradaDePagina>
        </main>
      </div>

      <BarraInferior />
    </div>
  );
}
