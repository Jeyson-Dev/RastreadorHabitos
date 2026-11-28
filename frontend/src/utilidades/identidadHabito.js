import {
  Apple,
  BedDouble,
  Bike,
  BookOpen,
  Brain,
  Code,
  Coffee,
  Droplets,
  Dumbbell,
  Flower2,
  Footprints,
  GraduationCap,
  Guitar,
  HeartPulse,
  Languages,
  Leaf,
  Music,
  Palette,
  PenLine,
  PiggyBank,
  Pill,
  Salad,
  Smartphone,
  Sun,
} from 'lucide-react';

const COLORES = 8;

// Color estable de un hábito: el mismo id siempre da el mismo color (1 a 8), en cualquier
// dispositivo y sin guardarlo en el servidor.
export function colorDeHabito(id = '') {
  let suma = 0;
  for (const caracter of id) suma = (suma * 31 + caracter.charCodeAt(0)) >>> 0;
  const n = (suma % COLORES) + 1;
  return { fondo: `var(--h${n}-fondo)`, tinta: `var(--h${n}-tinta)` };
}

// Palabras del nombre → ícono. Se compara sin tildes y por raíz, en orden: gana la primera regla.
const REGLAS = [
  [/\b(le(er|o|ctura)|libro|novela)/, BookOpen],
  [/\b(camin|pase|trot|corr|pasos)/, Footprints],
  [/\b(bici|ciclis|pedal)/, Bike],
  [/\b(agua|hidrat|beb)/, Droplets],
  [/\b(medit|respir|mindful|calma)/, Brain],
  [/\b(dorm|sueno|descans|siesta|acostar)/, BedDouble],
  [/\b(gim|pesas|ejercic|entren|flexion|abdomin|fuerza)/, Dumbbell],
  [/\b(yoga|estir|pilates)/, Flower2],
  [/\b(estudi|curso|clase|repas|tarea|universidad)/, GraduationCap],
  [/\b(idioma|ingles|frances|aleman|portugues|duolingo)/, Languages],
  [/\b(program|codig|code)/, Code],
  [/\b(escrib|diario|journal|notas)/, PenLine],
  [/\b(fruta|verdura|ensalada|vegetal|comer sano|dieta)/, Salad],
  [/\b(cocin|desayun|almuerz|cena|comida)/, Apple],
  [/\b(ahorr|dinero|finanz|gasto|presupuest)/, PiggyBank],
  [/\b(guitarra|piano|violin|instrumento)/, Guitar],
  [/\b(music|cantar|escuchar)/, Music],
  [/\b(dibuj|pint|arte)/, Palette],
  [/\b(pastilla|medic|vitamina)/, Pill],
  [/\b(cafe|te\b)/, Coffee],
  [/\b(pantalla|celular|movil|redes)/, Smartphone],
  [/\b(planta|jardin|regar)/, Leaf],
  [/\b(sol|madrug|temprano)/, Sun],
  [/\b(salud|corazon|cardio|presion)/, HeartPulse],
];

const sinTildes = (texto) => texto.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();

// Ícono deducido del nombre, o null si ninguna palabra coincide (entonces se muestra la inicial).
export function iconoDeHabito(nombre = '') {
  const texto = sinTildes(nombre);
  return REGLAS.find(([patron]) => patron.test(texto))?.[1] ?? null;
}

export function inicialDe(nombre = '') {
  return nombre.trim().charAt(0).toUpperCase() || '·';
}
