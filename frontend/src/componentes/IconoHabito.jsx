import { createElement } from 'react';
import { colorDeHabito, iconoDeHabito, inicialDe } from '../utilidades/identidadHabito';

const TAMANOS = {
  sm: { caja: 'size-8 rounded-lg text-sm', icono: 'size-4' },
  md: { caja: 'size-10 rounded-xl text-base', icono: 'size-5' },
  lg: { caja: 'size-12 rounded-2xl text-lg', icono: 'size-6' },
};

// Identidad visual de un hábito: color estable por id e ícono deducido del nombre (o su inicial).
// Es decorativo: el nombre siempre va escrito al lado.
export default function IconoHabito({ habito, tamano = 'md', className = '' }) {
  const { fondo, tinta } = colorDeHabito(habito.id);
  // El ícono sale de una tabla fija de lucide-react (no se crea en cada render).
  const icono = iconoDeHabito(habito.nombre);
  const medidas = TAMANOS[tamano];
  return (
    <span
      aria-hidden="true"
      className={`titulo inline-flex shrink-0 items-center justify-center font-semibold ${medidas.caja} ${habito.archivado ? 'opacity-60 grayscale' : ''} ${className}`}
      style={{ backgroundColor: fondo, color: tinta }}
    >
      {icono ? createElement(icono, { className: medidas.icono, strokeWidth: 2.2 }) : inicialDe(habito.nombre)}
    </span>
  );
}
