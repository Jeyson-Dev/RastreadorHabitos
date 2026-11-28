import { useId } from 'react';

// Campo de formulario con etiqueta visible, ayuda y error asociados por aria-describedby.
export default function Campo({ etiqueta, ayuda, error, id, className = '', ...entrada }) {
  const generado = useId();
  const idCampo = id ?? generado;
  const idAyuda = ayuda ? `${idCampo}-ayuda` : undefined;
  const idError = error ? `${idCampo}-error` : undefined;

  return (
    <div className={`flex flex-col gap-1.5 ${className}`}>
      <label htmlFor={idCampo} className="text-sm font-medium text-texto">
        {etiqueta}
      </label>
      <input
        {...entrada}
        id={idCampo}
        aria-invalid={error ? true : undefined}
        aria-describedby={[idAyuda, idError].filter(Boolean).join(' ') || undefined}
        className={`h-10 rounded-xl border bg-superficie/80 px-3 text-sm text-texto placeholder:text-suave/70 focus:border-acento focus:outline-none focus:ring-2 focus:ring-acento/30 ${error ? 'border-peligro' : 'border-borde'}`}
      />
      {ayuda && !error && (
        <p id={idAyuda} className="text-xs text-suave">
          {ayuda}
        </p>
      )}
      {error && (
        <p id={idError} className="text-xs font-medium text-peligro">
          {error}
        </p>
      )}
    </div>
  );
}
