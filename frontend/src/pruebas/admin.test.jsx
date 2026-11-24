import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { abrir, conSesionGuardada, llamadasA, simularApi, usuarioDePrueba } from './montaje';

const YO = usuarioDePrueba('Administrador', { id: 'a1', nombreCompleto: 'Ana Admin', email: 'ana@correo.com' });
const USUARIOS = [
  { id: 'a1', email: 'ana@correo.com', nombreCompleto: 'Ana Admin', rol: 'Administrador', estado: 'Activo' },
  { id: 'u2', email: 'luis@correo.com', nombreCompleto: 'Luis Pérez', rol: 'Estandar', estado: 'Activo' },
];
const SOLICITUD = (extra = {}) => ({ id: 's1', solicitanteId: 'u2', permiso: 'LIMITE_HABITOS_AMPLIADO', estado: 'Pendiente', fechaCreacionUtc: new Date().toISOString(), ...extra });

function api(extra = {}) {
  return simularApi({
    '/api/sesion/usuario': [200, YO],
    '/api/notificaciones': [200, { noLeidas: 0, notificaciones: [] }],
    '/api/usuarios': [200, USUARIOS],
    ...extra,
  });
}

beforeEach(() => conSesionGuardada());
afterEach(() => vi.unstubAllGlobals());

describe('solicitudes de permiso', () => {
  it('muestra quién pidió, no deja resolver la propia y aprueba otra tras confirmar', async () => {
    // Preparar
    const falso = api({
      '/api/solicitudes-permiso/pendientes': [200, [SOLICITUD(), SOLICITUD({ id: 's2', solicitanteId: 'a1' })]],
      'POST /api/solicitudes-permiso/s1/aprobar': [200, SOLICITUD({ estado: 'Aplicada' })],
    });
    abrir('/admin/solicitudes');
    const persona = userEvent.setup();

    // Actuar
    expect(await screen.findByText('Luis Pérez (luis@correo.com)')).toBeInTheDocument();
    expect(screen.getByText('Es tuya: debe resolverla otro administrador.')).toBeInTheDocument();
    await persona.click(screen.getByRole('button', { name: 'Aprobar' }));
    await persona.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Aprobar' }));

    // Verificar
    expect(await screen.findByText('Aprobaste la solicitud de Luis Pérez (luis@correo.com).')).toBeInTheDocument();
    expect(llamadasA(falso, 'POST', '/api/solicitudes-permiso/s1/aprobar')).toHaveLength(1);
  });

  it.each([
    ['vacío', ''],
    ['en blanco', '    '],
  ])('no rechaza con el motivo %s', async (_caso, motivo) => {
    // Preparar
    const falso = api({ '/api/solicitudes-permiso/pendientes': [200, [SOLICITUD()]] });
    abrir('/admin/solicitudes');
    const persona = userEvent.setup();

    // Actuar
    await persona.click(await screen.findByRole('button', { name: 'Rechazar' }));
    const dialogo = await screen.findByRole('dialog');
    if (motivo) await persona.type(within(dialogo).getByLabelText('Motivo del rechazo'), motivo);
    await persona.click(within(dialogo).getByRole('button', { name: 'Rechazar' }));

    // Verificar
    expect(within(dialogo).getByText('Escribe el motivo.')).toBeInTheDocument();
    expect(llamadasA(falso, 'POST', '/api/solicitudes-permiso/s1/rechazar')).toHaveLength(0);
  });

  it('rechaza con el motivo escrito', async () => {
    // Preparar
    const falso = api({
      '/api/solicitudes-permiso/pendientes': [200, [SOLICITUD()]],
      'POST /api/solicitudes-permiso/s1/rechazar': [200, SOLICITUD({ estado: 'Rechazada' })],
    });
    abrir('/admin/solicitudes');
    const persona = userEvent.setup();

    // Actuar
    await persona.click(await screen.findByRole('button', { name: 'Rechazar' }));
    const dialogo = await screen.findByRole('dialog');
    await persona.type(within(dialogo).getByLabelText('Motivo del rechazo'), '  No corresponde.  ');
    await persona.click(within(dialogo).getByRole('button', { name: 'Rechazar' }));

    // Verificar
    expect(await screen.findByText(/Rechazaste la solicitud/)).toBeInTheDocument();
    expect(llamadasA(falso, 'POST', '/api/solicitudes-permiso/s1/rechazar')[0].cuerpo).toEqual({ motivo: 'No corresponde.' });
  });
});

describe('usuarios y roles', () => {
  it('sobre uno mismo solo ofrece restablecer la contraseña y a otro lo hace administrador', async () => {
    // Preparar
    const falso = api({ 'PUT /api/usuarios/u2/rol': [200, { mensaje: 'Rol actualizado.' }] });
    abrir('/admin/usuarios');
    const persona = userEvent.setup();

    // Actuar
    const propias = await screen.findByRole('group', { name: 'Acciones sobre Ana Admin' });
    expect(within(propias).getAllByRole('button').map((b) => b.textContent)).toEqual(['Restablecer contraseña']);
    await persona.click(within(screen.getByRole('group', { name: 'Acciones sobre Luis Pérez' })).getByRole('button', { name: 'Hacer administrador' }));
    await persona.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Hacer administrador' }));

    // Verificar
    expect(await screen.findByText('Luis Pérez ahora es administrador.')).toBeInTheDocument();
    expect(llamadasA(falso, 'PUT', '/api/usuarios/u2/rol')[0].cuerpo).toEqual({ rol: 'Administrador' });
  });

  it('muestra el rechazo del servidor dentro del diálogo', async () => {
    // Preparar
    api({ 'POST /api/usuarios/u2/desactivar': [403, { error: 'No se puede desactivar a este usuario.' }] });
    abrir('/admin/usuarios');
    const persona = userEvent.setup();

    // Actuar
    await persona.click(within(await screen.findByRole('group', { name: 'Acciones sobre Luis Pérez' })).getByRole('button', { name: 'Desactivar' }));
    const dialogo = await screen.findByRole('dialog');
    await persona.click(within(dialogo).getByRole('button', { name: 'Desactivar' }));

    // Verificar
    expect(await within(dialogo).findByText('No se puede desactivar a este usuario.')).toBeInTheDocument();
  });
});

describe('cola de correos y auditoría', () => {
  it('filtra la cola por estado y muestra el último error', async () => {
    // Preparar
    const falso = api({
      '/api/correos': (_o, url) => [
        200,
        url.includes('estado=Fallido')
          ? [{ id: 'c1', destinatario: 'x@correo.com', asunto: 'Activa tu cuenta', estado: 'Fallido', intentos: 5, fechaCreacionUtc: '2026-11-23T10:00:00', ultimoError: 'SMTP no responde.' }]
          : [],
      ],
    });
    abrir('/admin/correos');

    // Actuar
    fireEvent.change(await screen.findByLabelText('Estado'), { target: { value: 'Fallido' } });

    // Verificar
    expect(await screen.findByText('Último error: SMTP no responde.')).toBeInTheDocument();
    expect(llamadasA(falso, 'GET', '/api/correos').some((l) => l.url.includes('estado=Fallido'))).toBe(true);
  });

  it('filtra la auditoría por entidad y no aplica un rango invertido', async () => {
    // Preparar
    const falso = api({
      '/api/auditoria': [
        200,
        { total: 1, registros: [{ id: 'r1', usuarioId: 'a1', accion: 'RolCambiado', entidad: 'Usuario', entidadId: 'u2', fechaUtc: '2026-11-23T10:00:00', valorAnterior: '{"rol":"Estandar"}', valorNuevo: '{"rol":"Administrador"}' }] },
      ],
    });
    abrir('/admin/auditoria');
    const persona = userEvent.setup();

    // Actuar
    expect(await screen.findByText('RolCambiado')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Entidad'), { target: { value: 'Usuario' } });
    await persona.click(screen.getByRole('button', { name: 'Filtrar' }));
    fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2026-12-01' } });
    fireEvent.change(screen.getByLabelText('Hasta'), { target: { value: '2026-11-01' } });
    await persona.click(screen.getByRole('button', { name: 'Filtrar' }));

    // Verificar
    expect(llamadasA(falso, 'GET', '/api/auditoria').some((l) => l.url.includes('entidad=Usuario'))).toBe(true);
    expect(screen.getByRole('alert')).toHaveTextContent('no puede ser posterior');
    expect(llamadasA(falso, 'GET', '/api/auditoria').some((l) => l.url.includes('2026-12-01'))).toBe(false);
  });
});

describe('documentos', () => {
  it('sube un archivo como multipart y lo elimina tras confirmar', async () => {
    // Preparar
    const doc = { id: 'd1', nombre: 'plan.pdf', tipo: 'application/pdf', tamanoBytes: 2048, fechaSubidaUtc: '2026-11-23T10:00:00', subidoPorId: 'a1' };
    const falso = api({
      '/api/documentos': [200, [doc]],
      'POST /api/documentos': [201, doc],
      'DELETE /api/documentos/d1': [200, { mensaje: 'Documento eliminado.' }],
    });
    abrir('/documentos');
    const persona = userEvent.setup();

    // Actuar
    await persona.upload(await screen.findByLabelText('Elegir archivo'), new File(['%PDF'], 'plan.pdf', { type: 'application/pdf' }));
    expect(await screen.findByText('Subiste «plan.pdf».')).toBeInTheDocument();
    await persona.click(screen.getByRole('button', { name: 'Eliminar plan.pdf' }));
    await persona.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Eliminar' }));

    // Verificar
    const subida = llamadasA(falso, 'POST', '/api/documentos')[0].cuerpo;
    expect(subida).toBeInstanceOf(FormData);
    expect(subida.get('archivo').name).toBe('plan.pdf');
    expect(await screen.findByText('Eliminaste «plan.pdf».')).toBeInTheDocument();
  });

  it('no sube un archivo vacío', async () => {
    // Preparar
    const falso = api({ '/api/documentos': [200, []] });
    abrir('/documentos');
    const persona = userEvent.setup();

    // Actuar
    await persona.upload(await screen.findByLabelText('Elegir archivo'), new File([], 'vacio.txt'));

    // Verificar
    expect(await screen.findByText('El archivo está vacío.')).toBeInTheDocument();
    expect(llamadasA(falso, 'POST', '/api/documentos')).toHaveLength(0);
  });
});
