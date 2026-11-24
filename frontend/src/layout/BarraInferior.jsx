import { useRef } from 'react';
import { Ellipsis, LogOut, X } from 'lucide-react';
import { NavLink } from 'react-router';
import { useSesion } from '../sesion/contextoSesion';
import EnlaceNavegacion from './EnlaceNavegacion';
import { RUTAS_BARRA_INFERIOR, SECCION_PERFIL, SECCIONES_ADMINISTRACION, SECCIONES_PRINCIPALES } from './navegacion';

// Navegación móvil: cuatro accesos de uso diario y «Más» con el resto (un <dialog> nativo, que
// atrapa el foco y se cierra con Escape).
export default function BarraInferior() {
  const { esAdministrador, cerrarSesion } = useSesion();
  const dialogo = useRef(null);
  const fijas = SECCIONES_PRINCIPALES.filter((s) => RUTAS_BARRA_INFERIOR.includes(s.a));
  const resto = SECCIONES_PRINCIPALES.filter((s) => !RUTAS_BARRA_INFERIOR.includes(s.a));
  const cerrar = () => dialogo.current?.close();

  return (
    <>
      <nav
        aria-label="Principal"
        className="fixed inset-x-0 bottom-0 z-20 border-t border-borde bg-superficie/95 pb-[env(safe-area-inset-bottom)] backdrop-blur md:hidden"
      >
        <ul className="grid grid-cols-5">
          {fijas.map(({ a, etiqueta, icono: Icono, fin }) => (
            <li key={a}>
              <NavLink
                to={a}
                end={fin}
                className={({ isActive }) =>
                  `flex h-16 flex-col items-center justify-center gap-1 text-[11px] font-medium ${isActive ? 'text-acento' : 'text-suave'}`
                }
              >
                <Icono className="size-5" aria-hidden="true" />
                <span className="max-w-full truncate px-1">{etiqueta === 'Notificaciones' ? 'Avisos' : etiqueta}</span>
              </NavLink>
            </li>
          ))}
          <li>
            <button
              type="button"
              onClick={() => dialogo.current?.showModal()}
              className="flex h-16 w-full flex-col items-center justify-center gap-1 text-[11px] font-medium text-suave"
              aria-haspopup="dialog"
            >
              <Ellipsis className="size-5" aria-hidden="true" />
              Más
            </button>
          </li>
        </ul>
      </nav>

      <dialog
        ref={dialogo}
        aria-labelledby="titulo-mas"
        onClick={(e) => e.target === dialogo.current && cerrar()}
        className="mt-auto mb-0 w-full max-w-none rounded-t-2xl border border-borde bg-superficie p-0 text-texto backdrop:bg-black/40 md:hidden"
      >
        <div className="flex items-center justify-between border-b border-borde px-4 py-3">
          <h2 id="titulo-mas" className="font-semibold">
            Más opciones
          </h2>
          <button
            type="button"
            onClick={cerrar}
            className="inline-flex size-9 items-center justify-center rounded-lg text-suave hover:bg-superficie-suave"
            aria-label="Cerrar"
          >
            <X className="size-5" aria-hidden="true" />
          </button>
        </div>
        <nav aria-label="Más opciones" className="flex flex-col gap-4 p-3 pb-[calc(env(safe-area-inset-bottom)+0.75rem)]">
          <ul className="flex flex-col gap-0.5">
            {[...resto, SECCION_PERFIL].map((seccion) => (
              <li key={seccion.a}>
                <EnlaceNavegacion seccion={seccion} alElegir={cerrar} />
              </li>
            ))}
          </ul>
          {esAdministrador && (
            <div className="flex flex-col gap-1">
              <p className="px-3 text-xs font-semibold tracking-wide text-suave uppercase">Administración</p>
              <ul className="flex flex-col gap-0.5">
                {SECCIONES_ADMINISTRACION.map((seccion) => (
                  <li key={seccion.a}>
                    <EnlaceNavegacion seccion={seccion} alElegir={cerrar} />
                  </li>
                ))}
              </ul>
            </div>
          )}
          <button
            type="button"
            onClick={() => {
              cerrar();
              cerrarSesion();
            }}
            className="flex h-10 items-center gap-3 rounded-lg px-3 text-sm font-medium text-peligro hover:bg-peligro-suave"
          >
            <LogOut className="size-[18px]" aria-hidden="true" />
            Cerrar sesión
          </button>
        </nav>
      </dialog>
    </>
  );
}
