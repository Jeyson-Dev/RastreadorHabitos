import { useEffect, useId, useRef } from 'react';

// Diálogo centrado y breve (confirmar o pedir un dato). <dialog> nativo: atrapa el foco, Escape o un
// toque fuera lo cierran y devuelve el foco al cerrar. El foco inicial va a [data-autofocus].
export default function Dialogo({ abierto, alCerrar, titulo, descripcion, children }) {
  const dialogo = useRef(null);
  const abiertoActual = useRef(abierto);
  const idTitulo = useId();
  const idDescripcion = useId();

  useEffect(() => {
    abiertoActual.current = abierto;
    const elemento = dialogo.current;
    if (!elemento) return;
    if (abierto && !elemento.open) {
      elemento.showModal();
      elemento.querySelector('[data-autofocus]')?.focus();
    }
    if (!abierto && elemento.open) elemento.close();
  }, [abierto]);

  return (
    <dialog
      ref={dialogo}
      aria-labelledby={idTitulo}
      aria-describedby={descripcion ? idDescripcion : undefined}
      onClose={() => abiertoActual.current && alCerrar()}
      onClick={(e) => e.target === dialogo.current && dialogo.current.close()}
      className="m-auto w-[calc(100%-2rem)] max-w-md rounded-2xl border border-borde bg-superficie p-0 text-texto backdrop:bg-black/40"
    >
      {abierto && (
        <div className="flex flex-col gap-4 p-6">
          <div className="flex flex-col gap-1">
            <h2 id={idTitulo} className="text-lg font-semibold">
              {titulo}
            </h2>
            {descripcion && (
              <p id={idDescripcion} className="text-sm text-suave">
                {descripcion}
              </p>
            )}
          </div>
          {children}
        </div>
      )}
    </dialog>
  );
}
