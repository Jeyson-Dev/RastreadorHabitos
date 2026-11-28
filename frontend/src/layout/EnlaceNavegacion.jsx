import { m, useReducedMotion } from 'motion/react';
import { NavLink } from 'react-router';
import { resortes } from '../utilidades/movimiento';

// Enlace de la barra lateral o del menú «Más». NavLink marca aria-current="page" en el activo. El
// fondo del activo es una sola pieza que se desliza de un enlace a otro (layoutId por grupo).
export default function EnlaceNavegacion({ seccion, alElegir, grupo = 'lateral' }) {
  const { a, etiqueta, icono: Icono, fin } = seccion;
  const reducir = useReducedMotion();
  return (
    <NavLink
      to={a}
      end={fin}
      onClick={alElegir}
      className={({ isActive }) =>
        `relative isolate flex h-10 items-center gap-3 rounded-xl px-3 text-sm font-medium transition-colors ${
          isActive ? 'text-acento' : 'text-suave hover:bg-superficie-suave/60 hover:text-texto'
        }`
      }
    >
      {({ isActive }) => (
        <>
          {isActive && (
            <m.span
              layoutId={reducir ? undefined : `activo-${grupo}`}
              transition={resortes.agil}
              className="absolute inset-0 -z-10 rounded-xl bg-acento-suave shadow-[inset_0_0_0_1px_color-mix(in_oklab,var(--acento)_18%,transparent)]"
              aria-hidden="true"
            />
          )}
          <Icono className="size-[18px] shrink-0" aria-hidden="true" />
          {etiqueta}
        </>
      )}
    </NavLink>
  );
}
