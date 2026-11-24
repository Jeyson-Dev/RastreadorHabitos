import { TriangleAlert } from 'lucide-react';
import { useRouteError } from 'react-router';

// Error inesperado al mostrar una pantalla: un mensaje comprensible y una salida, nunca la traza.
// El detalle queda en la consola del navegador para quien desarrolla.
export default function PaginaError() {
  const error = useRouteError();
  console.error(error);

  return (
    <main className="flex min-h-dvh items-center justify-center p-6">
      <div role="alert" className="flex max-w-md flex-col items-center gap-4 text-center">
        <TriangleAlert className="size-10 text-aviso" aria-hidden="true" />
        <h1 className="text-xl font-semibold">Algo salió mal al mostrar esta pantalla</h1>
        <p className="text-sm text-suave">Recarga la página para intentarlo de nuevo. Si vuelve a pasar, regresa al inicio.</p>
        <div className="flex gap-3">
          <button
            type="button"
            onClick={() => window.location.reload()}
            className="h-10 rounded-lg bg-acento px-4 text-sm font-medium text-sobre-acento hover:bg-acento-hover"
          >
            Recargar
          </button>
          <a href="/" className="inline-flex h-10 items-center rounded-lg border border-borde px-4 text-sm font-medium hover:bg-superficie-suave">
            Ir al inicio
          </a>
        </div>
      </div>
    </main>
  );
}
