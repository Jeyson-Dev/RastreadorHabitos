import { DIAS_SEMANA, ENTRE_SEMANA, TODOS_LOS_DIAS } from '../utilidades/fechas';

// Siete botones que se activan y desactivan (aria-pressed), con atajos para todos los días y
// entre semana. «valor» es la lista de días elegidos (1 = lunes … 7 = domingo).
export default function SelectorDias({ valor, alCambiar, error, idError }) {
  const alternar = (dia) => alCambiar(valor.includes(dia) ? valor.filter((d) => d !== dia) : [...valor, dia].sort((a, b) => a - b));

  return (
    <fieldset aria-describedby={error ? idError : undefined} className="flex flex-col gap-2">
      <legend className="mb-1.5 text-sm font-medium text-texto">Días de la semana</legend>
      <div className="flex flex-wrap gap-1.5">
        {DIAS_SEMANA.map(({ numero, corto, nombre }) => {
          const elegido = valor.includes(numero);
          return (
            <button
              key={numero}
              type="button"
              onClick={() => alternar(numero)}
              aria-pressed={elegido}
              aria-label={nombre}
              className={`inline-flex size-10 items-center justify-center rounded-full border text-sm font-semibold transition-colors ${
                elegido ? 'border-acento bg-acento text-sobre-acento' : 'border-borde bg-superficie text-suave hover:text-texto'
              }`}
            >
              {corto}
            </button>
          );
        })}
      </div>
      <div className="flex gap-3 text-xs">
        <button type="button" onClick={() => alCambiar([...TODOS_LOS_DIAS])} className="font-medium text-acento hover:underline">
          Todos
        </button>
        <button type="button" onClick={() => alCambiar([...ENTRE_SEMANA])} className="font-medium text-acento hover:underline">
          Entre semana
        </button>
        {valor.length > 0 && (
          <button type="button" onClick={() => alCambiar([])} className="text-suave hover:text-texto">
            Quitar todos
          </button>
        )}
      </div>
      {error && (
        <p id={idError} className="text-xs font-medium text-peligro">
          {error}
        </p>
      )}
    </fieldset>
  );
}
