import { CalendarCheck } from 'lucide-react';
import EstadoVacio from '../componentes/EstadoVacio';

// Marcador de las secciones que llegan en los próximos bloques.
export default function EnConstruccion({ titulo }) {
  return (
    <EstadoVacio icono={CalendarCheck} titulo={titulo}>
      Esta sección estará disponible pronto.
    </EstadoVacio>
  );
}
