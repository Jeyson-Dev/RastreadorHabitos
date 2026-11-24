import { Flame } from 'lucide-react';

// Racha actual con su llama; en cero se ve apagada. El texto completo queda para lectores de pantalla.
export default function Racha({ dias, className = '' }) {
  const activa = dias > 0;
  return (
    <span
      className={`numeros inline-flex items-center gap-1 text-sm font-semibold ${activa ? 'text-racha' : 'text-suave'} ${className}`}
      title={`Racha actual: ${dias} ${dias === 1 ? 'día' : 'días'}`}
    >
      <Flame className="size-4" aria-hidden="true" />
      {dias}
      <span className="sr-only">{dias === 1 ? ' día de racha' : ' días de racha'}</span>
    </span>
  );
}
