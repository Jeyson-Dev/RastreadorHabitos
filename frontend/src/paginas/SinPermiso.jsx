import { ShieldAlert } from 'lucide-react';
import { Link } from 'react-router';
import EstadoVacio from '../componentes/EstadoVacio';

// Límite de acceso: explica por qué no se ve y ofrece volver a un lugar seguro.
export default function SinPermiso() {
  return (
    <div className="mx-auto max-w-lg p-6">
      <EstadoVacio
        icono={ShieldAlert}
        titulo="Esta sección es solo para administradores"
        accion={
          <Link to="/" className="text-sm font-medium text-acento hover:underline">
            Volver a Hoy
          </Link>
        }
      >
        Si necesitas acceso, pídele a un administrador que revise tu cuenta.
      </EstadoVacio>
    </div>
  );
}
