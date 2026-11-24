import { Monitor, Moon, Sun } from 'lucide-react';
import { useTema } from './contextoTema';

const OPCIONES = {
  sistema: { icono: Monitor, nombre: 'Tema del sistema', siguiente: 'claro' },
  claro: { icono: Sun, nombre: 'Tema claro', siguiente: 'oscuro' },
  oscuro: { icono: Moon, nombre: 'Tema oscuro', siguiente: 'sistema' },
};

// Un botón que recorre sistema → claro → oscuro; el nombre accesible dice el actual y el siguiente.
export default function InterruptorTema() {
  const { preferencia, cambiarPreferencia } = useTema();
  const actual = OPCIONES[preferencia];
  const Icono = actual.icono;
  const siguiente = OPCIONES[actual.siguiente];

  return (
    <button
      type="button"
      onClick={() => cambiarPreferencia(actual.siguiente)}
      className="inline-flex size-10 items-center justify-center rounded-lg text-suave transition-colors hover:bg-superficie-suave hover:text-texto"
      aria-label={`${actual.nombre}. Cambiar a ${siguiente.nombre.toLowerCase()}`}
      title={`${actual.nombre} (cambiar a ${siguiente.nombre.toLowerCase()})`}
    >
      <Icono className="size-5" aria-hidden="true" />
    </button>
  );
}
