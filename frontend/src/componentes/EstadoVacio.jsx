import { Inbox } from 'lucide-react';

// Estado vacío: explica qué falta y ofrece la siguiente acción útil.
export default function EstadoVacio({ icono: Icono = Inbox, titulo, children, accion }) {
  return (
    <div className="flex flex-col items-center gap-3 rounded-xl border border-dashed border-borde px-6 py-12 text-center">
      <Icono className="size-8 text-suave" aria-hidden="true" />
      <div className="flex max-w-sm flex-col gap-1">
        <p className="font-semibold text-texto">{titulo}</p>
        {children && <p className="text-sm text-suave">{children}</p>}
      </div>
      {accion}
    </div>
  );
}
