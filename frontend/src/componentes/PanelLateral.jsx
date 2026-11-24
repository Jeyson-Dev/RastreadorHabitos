import { useEffect, useId, useRef } from 'react';
import { X } from 'lucide-react';

// Panel sobre la pantalla: desde la derecha en escritorio y como hoja desde abajo en móvil. Es un
// <dialog> nativo: atrapa el foco, se cierra con Escape o tocando fuera y devuelve el foco al cerrar.
export default function PanelLateral({ abierto, alCerrar, titulo, children }) {
  const dialogo = useRef(null);
  const idTitulo = useId();

  useEffect(() => {
    const elemento = dialogo.current;
    if (!elemento) return;
    if (abierto && !elemento.open) elemento.showModal();
    if (!abierto && elemento.open) elemento.close();
  }, [abierto]);

  return (
    <dialog
      ref={dialogo}
      aria-labelledby={idTitulo}
      onClose={alCerrar}
      onClick={(e) => e.target === dialogo.current && alCerrar()}
      className="mt-auto mb-0 max-h-[90dvh] w-full max-w-none rounded-t-2xl border border-borde bg-superficie p-0 text-texto backdrop:bg-black/40 md:mt-0 md:mr-0 md:ml-auto md:h-dvh md:max-h-none md:w-[28rem] md:rounded-none md:border-y-0 md:border-r-0"
    >
      {abierto && (
        <div className="flex h-full flex-col">
          <div className="flex items-center justify-between gap-3 border-b border-borde px-5 py-4">
            <h2 id={idTitulo} className="truncate text-lg font-semibold">
              {titulo}
            </h2>
            <button
              type="button"
              onClick={alCerrar}
              className="inline-flex size-9 shrink-0 items-center justify-center rounded-lg text-suave hover:bg-superficie-suave hover:text-texto"
              aria-label="Cerrar"
            >
              <X className="size-5" aria-hidden="true" />
            </button>
          </div>
          <div className="flex-1 overflow-y-auto px-5 py-5 pb-[calc(env(safe-area-inset-bottom)+1.25rem)]">{children}</div>
        </div>
      )}
    </dialog>
  );
}
