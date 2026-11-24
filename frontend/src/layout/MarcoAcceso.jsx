import { Flame, Target } from 'lucide-react';
import { useEffect } from 'react';
import { Outlet, useMatches } from 'react-router';
import InterruptorTema from '../tema/InterruptorTema';

// Pantallas de acceso: en escritorio, panel de marca a la izquierda y formulario a la derecha; en
// móvil, solo el formulario con el logo arriba.
export default function MarcoAcceso() {
  const coincidencias = useMatches();
  const titulo = [...coincidencias].reverse().find((c) => c.handle?.titulo)?.handle.titulo;

  useEffect(() => {
    document.title = titulo ? `${titulo} · Rastreador de hábitos` : 'Rastreador de hábitos';
  }, [titulo]);

  return (
    <div className="grid min-h-dvh lg:grid-cols-[minmax(0,5fr)_minmax(0,6fr)]">
      <aside className="relative hidden flex-col justify-between overflow-hidden bg-emerald-900 p-10 text-emerald-50 lg:flex">
        <div className="flex items-center gap-2.5 font-semibold">
          <img src="/icono.svg" alt="" className="size-8 rounded-lg ring-1 ring-emerald-700" />
          Rastreador de hábitos
        </div>

        <div className="flex max-w-md flex-col gap-8">
          <div className="flex flex-col gap-3">
            <p className="text-3xl leading-tight font-semibold text-white">Pequeños hábitos, grandes metas.</p>
            <p className="text-emerald-100/80">
              Marca cada día lo que cumpliste, cuida tus rachas y mira cómo avanzan tus metas.
            </p>
          </div>

          {/* Vista previa ilustrativa de lo que se ve dentro de la app. */}
          <div aria-hidden="true" className="flex flex-col gap-3 rounded-xl border border-emerald-700/60 bg-emerald-950/40 p-5">
            <div className="flex items-center justify-between">
              <span className="font-medium text-white">Leer 20 minutos</span>
              <span className="numeros inline-flex items-center gap-1 text-sm font-semibold text-amber-300">
                <Flame className="size-4" />
                12 días
              </span>
            </div>
            <div className="h-px bg-emerald-700/50" />
            <div className="flex items-center gap-2 text-sm text-emerald-100/90">
              <Target className="size-4" />
              Leer 20 veces antes de fin de mes
            </div>
            <div className="h-2 overflow-hidden rounded-full bg-emerald-950">
              <div className="h-full w-[70%] rounded-full bg-emerald-400" />
            </div>
            <span className="numeros self-end text-xs text-emerald-100/70">14 de 20</span>
          </div>
        </div>

        <p className="text-xs text-emerald-100/60">Programación III · ITLA</p>
      </aside>

      <div className="flex flex-col">
        <div className="flex items-center justify-between p-4">
          <img src="/icono.svg" alt="Rastreador de hábitos" className="size-8 lg:invisible" />
          <InterruptorTema />
        </div>
        <main className="flex flex-1 items-start justify-center px-4 pt-4 pb-12 sm:items-center sm:pt-0">
          <div className="w-full max-w-sm">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
