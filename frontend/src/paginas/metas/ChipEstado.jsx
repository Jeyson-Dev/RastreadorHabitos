import { PRESENTACION_ESTADO } from './presentacion';

// Chip con el estado de la meta (texto, no solo color).
export default function ChipEstado({ estado }) {
  const { texto, chip } = PRESENTACION_ESTADO[estado] ?? { texto: estado, chip: 'bg-superficie-suave text-suave' };
  return (
    <span className={`inline-flex shrink-0 items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium ${chip}`}>
      <span className="size-1.5 rounded-full bg-current" aria-hidden="true" />
      {texto}
    </span>
  );
}
