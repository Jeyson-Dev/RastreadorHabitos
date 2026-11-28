import { Inbox } from 'lucide-react';

// Estado vacío: explica qué falta y ofrece la siguiente acción útil. El ícono flota sobre un halo
// del color de acento.
export default function EstadoVacio({ icono: Icono = Inbox, titulo, children, accion }) {
  return (
    <div className="vidrio flex flex-col items-center gap-4 rounded-[1.25rem] px-6 py-14 text-center">
      <span className="relative isolate inline-flex size-16 items-center justify-center rounded-2xl bg-acento-suave text-acento" aria-hidden="true">
        <span className="absolute inset-0 -z-10 rounded-2xl bg-acento/25 blur-xl" />
        <Icono className="size-7" />
      </span>
      <div className="flex max-w-sm flex-col gap-1.5">
        <p className="titulo text-lg font-semibold text-texto">{titulo}</p>
        {children && <p className="text-sm text-suave">{children}</p>}
      </div>
      {accion}
    </div>
  );
}
