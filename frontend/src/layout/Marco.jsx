import { useEffect } from 'react';
import { Bell } from 'lucide-react';
import { Link, Outlet, useMatches } from 'react-router';
import InterruptorTema from '../tema/InterruptorTema';
import BarraInferior from './BarraInferior';
import BarraLateral from './BarraLateral';

// Marco de las pantallas con sesión: barra lateral (escritorio) o inferior (móvil), encabezado con
// el título de la sección, avisos y tema, y el contenido. El título sale del «handle» de la ruta.
export default function Marco() {
  const coincidencias = useMatches();
  const titulo = [...coincidencias].reverse().find((c) => c.handle?.titulo)?.handle.titulo ?? '';

  useEffect(() => {
    document.title = titulo ? `${titulo} · Rastreador de hábitos` : 'Rastreador de hábitos';
  }, [titulo]);

  return (
    <div className="min-h-dvh">
      <a
        href="#contenido"
        className="sr-only z-50 rounded-lg bg-acento px-4 py-2 text-sobre-acento focus:not-sr-only focus:fixed focus:top-3 focus:left-3"
      >
        Saltar al contenido
      </a>
      <BarraLateral />

      <div className="flex min-h-dvh flex-col md:pl-64">
        <header className="sticky top-0 z-10 flex h-16 items-center gap-3 border-b border-borde bg-fondo/90 px-4 backdrop-blur md:px-8">
          <img src="/icono.svg" alt="" className="size-7 md:hidden" />
          <h1 className="flex-1 truncate text-lg font-semibold">{titulo}</h1>
          <Link
            to="/notificaciones"
            className="inline-flex size-10 items-center justify-center rounded-lg text-suave hover:bg-superficie-suave hover:text-texto"
            aria-label="Notificaciones"
            title="Notificaciones"
          >
            <Bell className="size-5" aria-hidden="true" />
          </Link>
          <InterruptorTema />
        </header>

        <main id="contenido" tabIndex={-1} className="mx-auto w-full max-w-5xl flex-1 px-4 pt-6 pb-24 md:px-8 md:pb-10">
          <Outlet />
        </main>
      </div>

      <BarraInferior />
    </div>
  );
}
