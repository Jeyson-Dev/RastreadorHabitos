import { useEffect, useId, useRef, useState } from 'react';
import { AnimatePresence, m, useReducedMotion } from 'motion/react';
import { Ellipsis } from 'lucide-react';
import { resortes, tokensMovimiento } from '../utilidades/movimiento';

// Menú «⋯» con el patrón de menú de ARIA: flechas para moverse, Escape o tocar fuera para cerrar, y
// el foco vuelve al botón. «acciones»: [{ etiqueta, icono, alElegir, peligroso }].
export default function MenuAcciones({ etiqueta, acciones }) {
  const [abierto, setAbierto] = useState(false);
  const boton = useRef(null);
  const menu = useRef(null);
  const idMenu = useId();
  const reducir = useReducedMotion();

  useEffect(() => {
    if (!abierto) return undefined;
    menu.current?.querySelector('[role="menuitem"]')?.focus();
    const alTocarFuera = (evento) => {
      if (!menu.current?.contains(evento.target) && !boton.current?.contains(evento.target)) setAbierto(false);
    };
    document.addEventListener('pointerdown', alTocarFuera);
    return () => document.removeEventListener('pointerdown', alTocarFuera);
  }, [abierto]);

  function cerrar({ devolverFoco = true } = {}) {
    setAbierto(false);
    if (devolverFoco) boton.current?.focus();
  }

  function alTeclear(evento) {
    const elementos = [...(menu.current?.querySelectorAll('[role="menuitem"]') ?? [])];
    const actual = elementos.indexOf(document.activeElement);
    const mover = { ArrowDown: 1, ArrowUp: -1 }[evento.key];
    if (mover) {
      evento.preventDefault();
      elementos[(actual + mover + elementos.length) % elementos.length]?.focus();
    } else if (evento.key === 'Home' || evento.key === 'End') {
      evento.preventDefault();
      elementos[evento.key === 'Home' ? 0 : elementos.length - 1]?.focus();
    } else if (evento.key === 'Escape') {
      evento.preventDefault();
      cerrar();
    } else if (evento.key === 'Tab') {
      cerrar({ devolverFoco: false });
    }
  }

  return (
    <div className="relative">
      <button
        ref={boton}
        type="button"
        aria-label={etiqueta}
        aria-haspopup="menu"
        aria-expanded={abierto}
        aria-controls={abierto ? idMenu : undefined}
        onClick={() => setAbierto((v) => !v)}
        className="inline-flex size-9 items-center justify-center rounded-xl text-suave transition-colors hover:bg-superficie-suave/70 hover:text-texto aria-expanded:bg-superficie-suave aria-expanded:text-texto"
      >
        <Ellipsis className="size-5" aria-hidden="true" />
      </button>
      {/* Opaco a propósito: un backdrop-filter dentro de otra superficie de vidrio no desenfoca. */}
      <AnimatePresence>
        {abierto && (
          <m.ul
            ref={menu}
            id={idMenu}
            role="menu"
            aria-label={etiqueta}
            onKeyDown={alTeclear}
            initial={{ opacity: 0, scale: reducir ? 1 : tokensMovimiento.escala.sutil, y: reducir ? 0 : -tokensMovimiento.distancia.xs }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: reducir ? 1 : tokensMovimiento.escala.sutil }}
            transition={reducir ? { duration: tokensMovimiento.duracion.rapida } : resortes.instante}
            className="absolute top-full right-0 z-30 mt-1.5 flex min-w-48 origin-top-right flex-col rounded-2xl border border-borde bg-superficie p-1.5 shadow-suave"
          >
            {acciones.map(({ etiqueta: texto, icono: Icono, alElegir, peligroso }) => (
              <li key={texto} role="none">
                <button
                  type="button"
                  role="menuitem"
                  tabIndex={-1}
                  onClick={() => {
                    cerrar({ devolverFoco: false });
                    alElegir();
                  }}
                  className={`flex h-10 w-full items-center gap-2.5 rounded-xl px-3 text-left text-sm font-medium transition-colors focus:outline-none ${
                    peligroso ? 'text-peligro hover:bg-peligro-suave focus:bg-peligro-suave' : 'text-texto hover:bg-superficie-suave focus:bg-superficie-suave'
                  }`}
                >
                  {Icono && <Icono className="size-4 shrink-0" aria-hidden="true" />}
                  {texto}
                </button>
              </li>
            ))}
          </m.ul>
        )}
      </AnimatePresence>
    </div>
  );
}
