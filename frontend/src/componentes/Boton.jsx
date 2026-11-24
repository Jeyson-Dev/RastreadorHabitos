import { LoaderCircle } from 'lucide-react';

const VARIANTES = {
  primario: 'bg-acento text-sobre-acento hover:bg-acento-hover',
  secundario: 'border border-borde bg-superficie text-texto hover:bg-superficie-suave',
  fantasma: 'text-suave hover:bg-superficie-suave hover:text-texto',
  peligro: 'bg-peligro text-white hover:opacity-90 dark:text-zinc-950',
};

const TAMANOS = {
  sm: 'h-8 px-3 text-sm gap-1.5',
  md: 'h-10 px-4 text-sm gap-2',
  lg: 'h-11 px-5 text-base gap-2',
};

// Botón base. Con «cargando», queda deshabilitado y anuncia que la acción está en curso.
export default function Boton({
  variante = 'primario',
  tamano = 'md',
  cargando = false,
  disabled,
  className = '',
  children,
  type = 'button',
  ...resto
}) {
  return (
    <button
      type={type}
      disabled={disabled || cargando}
      aria-busy={cargando || undefined}
      className={`inline-flex items-center justify-center rounded-lg font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-60 ${VARIANTES[variante]} ${TAMANOS[tamano]} ${className}`}
      {...resto}
    >
      {cargando && <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />}
      {children}
    </button>
  );
}
