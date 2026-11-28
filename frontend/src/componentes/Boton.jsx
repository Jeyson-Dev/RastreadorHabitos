import { LoaderCircle } from 'lucide-react';

const VARIANTES = {
  primario:
    'bg-[linear-gradient(180deg,color-mix(in_oklab,var(--acento)_88%,white),var(--acento))] text-sobre-acento shadow-acento hover:brightness-105',
  secundario: 'vidrio text-texto hover:bg-vidrio-fuerte',
  fantasma: 'text-suave hover:bg-superficie-suave/70 hover:text-texto',
  peligro: 'bg-peligro text-white shadow-[0_8px_20px_-10px_var(--peligro)] hover:brightness-105 dark:text-[#2a0a12]',
};

const TAMANOS = {
  sm: 'h-8 px-3 text-sm gap-1.5',
  md: 'h-10 px-4 text-sm gap-2',
  lg: 'h-12 px-5 text-base gap-2',
};

// Botón base. Se hunde levemente al presionarlo. Con «cargando», queda deshabilitado y anuncia que la
// acción está en curso.
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
      className={`inline-flex items-center justify-center rounded-xl font-medium transition-[transform,filter,background-color,color] duration-150 ease-out active:scale-[0.97] disabled:cursor-not-allowed disabled:opacity-60 disabled:active:scale-100 ${VARIANTES[variante]} ${TAMANOS[tamano]} ${className}`}
      {...resto}
    >
      {cargando && <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />}
      {children}
    </button>
  );
}
