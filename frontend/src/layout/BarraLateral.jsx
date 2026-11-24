import { LogOut } from 'lucide-react';
import { Link } from 'react-router';
import { useSesion } from '../sesion/contextoSesion';
import EnlaceNavegacion from './EnlaceNavegacion';
import { SECCIONES_ADMINISTRACION, SECCIONES_PRINCIPALES } from './navegacion';

// Navegación de escritorio: fija a la izquierda, con el grupo de administración según el rol.
export default function BarraLateral() {
  const { usuario, esAdministrador, cerrarSesion } = useSesion();

  return (
    <aside className="fixed inset-y-0 left-0 hidden w-64 flex-col border-r border-borde bg-superficie md:flex">
      <Link to="/" className="flex h-16 items-center gap-2.5 px-5 font-semibold">
        <img src="/icono.svg" alt="" className="size-7" />
        Rastreador de hábitos
      </Link>

      <nav aria-label="Principal" className="flex flex-1 flex-col gap-6 overflow-y-auto px-3 py-2">
        <ul className="flex flex-col gap-0.5">
          {SECCIONES_PRINCIPALES.map((seccion) => (
            <li key={seccion.a}>
              <EnlaceNavegacion seccion={seccion} />
            </li>
          ))}
        </ul>

        {esAdministrador && (
          <div className="flex flex-col gap-1">
            <p id="titulo-administracion" className="px-3 text-xs font-semibold tracking-wide text-suave uppercase">
              Administración
            </p>
            <ul aria-labelledby="titulo-administracion" className="flex flex-col gap-0.5">
              {SECCIONES_ADMINISTRACION.map((seccion) => (
                <li key={seccion.a}>
                  <EnlaceNavegacion seccion={seccion} />
                </li>
              ))}
            </ul>
          </div>
        )}
      </nav>

      <div className="flex items-center gap-3 border-t border-borde p-3">
        <Link to="/perfil" className="flex min-w-0 flex-1 flex-col rounded-lg px-2 py-1.5 hover:bg-superficie-suave">
          <span className="truncate text-sm font-medium">{usuario?.nombreCompleto}</span>
          <span className="truncate text-xs text-suave">{usuario?.email}</span>
        </Link>
        <button
          type="button"
          onClick={() => cerrarSesion()}
          className="inline-flex size-9 shrink-0 items-center justify-center rounded-lg text-suave hover:bg-superficie-suave hover:text-texto"
          aria-label="Cerrar sesión"
          title="Cerrar sesión"
        >
          <LogOut className="size-[18px]" aria-hidden="true" />
        </button>
      </div>
    </aside>
  );
}
