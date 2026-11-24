import { LoaderCircle } from 'lucide-react';

// Indicador de carga con texto para lectores de pantalla. «pantalla» lo centra en todo el alto.
export default function Cargando({ texto = 'Cargando…', pantalla = false }) {
  return (
    <div
      role="status"
      className={`flex items-center justify-center gap-2 text-sm text-suave ${pantalla ? 'min-h-dvh' : 'py-12'}`}
    >
      <LoaderCircle className="size-5 animate-spin" aria-hidden="true" />
      <span>{texto}</span>
    </div>
  );
}
