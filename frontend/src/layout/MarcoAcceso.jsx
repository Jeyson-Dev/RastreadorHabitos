import { Check, Flame, Target } from 'lucide-react';
import { useEffect } from 'react';
import { m } from 'motion/react';
import { Outlet, useMatches } from 'react-router';
import InterruptorTema from '../tema/InterruptorTema';
import { useEntrada } from '../utilidades/movimiento';

// Pantallas de acceso: en escritorio, panel de marca a la izquierda (degradado cálido con una vista
// previa de vidrio) y formulario a la derecha sobre el fondo ambiental; en móvil, solo el formulario.
export default function MarcoAcceso() {
  const coincidencias = useMatches();
  const titulo = [...coincidencias].reverse().find((c) => c.handle?.titulo)?.handle.titulo;
  const entrada = useEntrada();

  useEffect(() => {
    document.title = titulo ? `${titulo} · Rastreador de hábitos` : 'Rastreador de hábitos';
  }, [titulo]);

  return (
    <div className="relative grid min-h-dvh lg:grid-cols-[minmax(0,5fr)_minmax(0,6fr)]">
      <div className="fondo-ambiente pointer-events-none fixed inset-0 -z-10" aria-hidden="true" />
      <aside className="relative isolate hidden flex-col justify-between overflow-hidden bg-[linear-gradient(150deg,#7a2f14_0%,#a84a26_45%,#c9772f_100%)] p-10 text-[#fff4ec] lg:flex">
        <div aria-hidden="true" className="absolute -top-24 -right-24 -z-10 size-96 rounded-full bg-[#f6c46b]/30 blur-3xl" />
        <div aria-hidden="true" className="absolute -bottom-32 -left-20 -z-10 size-[28rem] rounded-full bg-[#4a1a08]/50 blur-3xl" />

        <div className="flex items-center gap-2.5">
          <img src="/icono.svg" alt="" className="size-9 rounded-xl ring-1 ring-white/30" />
          <span className="titulo text-lg font-semibold">Rastreador de hábitos</span>
        </div>

        <div className="flex max-w-md flex-col gap-8">
          <div className="flex flex-col gap-3">
            <p className="titulo text-4xl leading-tight font-semibold text-white">Pequeños hábitos, grandes metas.</p>
            <p className="text-[#ffe9dc]/85">Marca cada día lo que cumpliste, cuida tus rachas y mira cómo avanzan tus metas.</p>
          </div>

          {/* Vista previa ilustrativa de lo que se ve dentro de la app. */}
          <div
            aria-hidden="true"
            className="flex flex-col gap-4 rounded-3xl border border-white/25 bg-white/12 p-5 shadow-[0_24px_60px_-24px_rgb(40_12_0/0.6)] backdrop-blur-xl"
          >
            <div className="flex items-center gap-3">
              <span className="inline-flex size-10 items-center justify-center rounded-xl bg-white/20">
                <Check className="size-5" />
              </span>
              <span className="flex-1 font-medium text-white">Leer 20 minutos</span>
              <span className="numeros inline-flex items-center gap-1 rounded-full bg-[#fbbf24]/20 px-2.5 py-1 text-sm font-semibold text-[#fde68a]">
                <Flame className="size-4" />
                12 días
              </span>
            </div>
            <div className="h-px bg-white/20" />
            <div className="flex items-center gap-2 text-sm text-[#ffe9dc]/90">
              <Target className="size-4" />
              Leer 20 veces antes de fin de mes
            </div>
            <div className="h-2 overflow-hidden rounded-full bg-black/20">
              <div className="h-full w-[70%] rounded-full bg-[linear-gradient(90deg,#fbbf24,#fff4ec)]" />
            </div>
            <span className="numeros self-end text-xs text-[#ffe9dc]/75">14 de 20</span>
          </div>
        </div>

        <p className="text-xs text-[#ffe9dc]/65">Programación III · ITLA</p>
      </aside>

      <div className="flex flex-col">
        <div className="flex items-center justify-between p-4">
          <img src="/icono.svg" alt="Rastreador de hábitos" className="size-8 lg:invisible" />
          <InterruptorTema />
        </div>
        <main className="flex flex-1 items-start justify-center px-4 pt-4 pb-12 sm:items-center sm:pt-0">
          <m.div {...entrada} className="vidrio w-full max-w-sm rounded-3xl p-6 sm:p-8">
            <Outlet />
          </m.div>
        </main>
      </div>
    </div>
  );
}
