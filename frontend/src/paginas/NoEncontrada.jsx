import { SearchX } from 'lucide-react';
import { Link } from 'react-router';
import EstadoVacio from '../componentes/EstadoVacio';

export default function NoEncontrada() {
  return (
    <div className="mx-auto max-w-lg p-6">
      <EstadoVacio
        icono={SearchX}
        titulo="No encontramos esta página"
        accion={
          <Link to="/" className="text-sm font-medium text-acento hover:underline">
            Ir al inicio
          </Link>
        }
      >
        Puede que el enlace esté mal escrito o que la página ya no exista.
      </EstadoVacio>
    </div>
  );
}
