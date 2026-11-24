import { afterEach, describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { requisitosContrasena, validarContrasena } from '../utilidades/contrasena';
import { abrir, conSesionGuardada, llamadasA, simularApi, usuarioDePrueba } from './montaje';

afterEach(() => vi.unstubAllGlobals());

describe('regla de contraseña (igual a la del servidor)', () => {
  it.each([
    ['', false],
    ['   ', false],
    ['abcdefgh', false],
    ['12345678', false],
    ['Hab2026', false],
    ['Habitos2026', true],
    ['ñandú2026', true],
  ])('«%s» cumple: %s', (contrasena, cumple) => {
    expect(validarContrasena(contrasena) === null).toBe(cumple);
  });

  it('rechaza más de 72 bytes aunque tenga letras y números', () => {
    expect(validarContrasena(`a1${'é'.repeat(36)}`)).toMatch(/72/);
  });

  it('marca cada requisito por separado', () => {
    expect(requisitosContrasena('abc1').map((r) => r.cumple)).toEqual([false, true, true]);
  });
});

describe('registro', () => {
  it('con datos válidos crea la cuenta y pide revisar el correo', async () => {
    // Preparar
    const api = simularApi({ 'POST /api/cuentas/registro': [201, { mensaje: 'Registro exitoso.' }] });
    abrir('/registro');
    const persona = userEvent.setup();

    // Actuar
    await persona.type(await screen.findByLabelText('Nombre completo'), '  Ana Pérez ');
    await persona.type(screen.getByLabelText('Correo'), 'ana@correo.com');
    await persona.type(screen.getByLabelText('Contraseña'), 'Habitos2026');
    await persona.click(screen.getByRole('button', { name: 'Crear cuenta' }));

    // Verificar
    expect(await screen.findByRole('heading', { name: 'Revisa tu correo' })).toBeInTheDocument();
    expect(llamadasA(api, 'POST', '/api/cuentas/registro')[0].cuerpo).toEqual({
      email: 'ana@correo.com',
      contrasena: 'Habitos2026',
      nombreCompleto: 'Ana Pérez',
    });
  });

  it.each([
    ['vacíos', '', '', ''],
    ['en blanco', '   ', '   ', '        '],
    ['mal formados', 'Ana', 'ana@', 'solo-letras'],
  ])('con datos %s no envía nada y señala cada campo', async (_caso, nombre, correo, contrasena) => {
    // Preparar
    const api = simularApi({});
    abrir('/registro');
    const persona = userEvent.setup();

    // Actuar
    if (nombre) await persona.type(await screen.findByLabelText('Nombre completo'), nombre);
    if (correo) await persona.type(screen.getByLabelText('Correo'), correo);
    if (contrasena) await persona.type(screen.getByLabelText('Contraseña'), contrasena);
    await persona.click(await screen.findByRole('button', { name: 'Crear cuenta' }));

    // Verificar
    expect(screen.getByLabelText('Correo')).toHaveAttribute('aria-invalid', 'true');
    expect(screen.getByLabelText('Contraseña')).toHaveAttribute('aria-invalid', 'true');
    expect(api).not.toHaveBeenCalled();
  });

  it('muestra el rechazo del servidor (correo ya registrado)', async () => {
    // Preparar
    simularApi({ 'POST /api/cuentas/registro': [409, { error: 'Ya existe una cuenta con ese correo.' }] });
    abrir('/registro');
    const persona = userEvent.setup();

    // Actuar
    await persona.type(await screen.findByLabelText('Nombre completo'), 'Ana');
    await persona.type(screen.getByLabelText('Correo'), 'ana@correo.com');
    await persona.type(screen.getByLabelText('Contraseña'), 'Habitos2026');
    await persona.click(screen.getByRole('button', { name: 'Crear cuenta' }));

    // Verificar
    expect(await screen.findByRole('alert')).toHaveTextContent('Ya existe una cuenta con ese correo.');
  });
});

describe('activación desde el enlace del correo', () => {
  it('activa la cuenta con una sola petición y ofrece iniciar sesión', async () => {
    // Preparar
    const api = simularApi({ '/api/cuentas/activar': [200, { mensaje: 'Cuenta activada. Ya puede iniciar sesión.' }] });

    // Actuar
    abrir('/activar?token=ABC123');

    // Verificar
    expect(await screen.findByRole('heading', { name: '¡Cuenta activada!' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Iniciar sesión' })).toHaveAttribute('href', '/iniciar-sesion');
    const llamadas = llamadasA(api, 'GET', '/api/cuentas/activar');
    expect(llamadas).toHaveLength(1);
    expect(llamadas[0].url).toBe('/api/cuentas/activar?token=ABC123');
  });

  it('con un enlace usado o vencido explica el motivo y permite pedir otro', async () => {
    // Preparar
    const api = simularApi({
      '/api/cuentas/activar': [400, { error: 'El enlace de activación no es válido, ya fue usado o venció.' }],
      'POST /api/cuentas/reenviar-activacion': [200, { mensaje: 'Si el correo está registrado, recibirá un nuevo enlace.' }],
    });
    abrir('/activar?token=VIEJO');
    const persona = userEvent.setup();

    // Actuar
    expect(await screen.findByText('El enlace de activación no es válido, ya fue usado o venció.')).toBeInTheDocument();
    await persona.type(screen.getByLabelText('Correo'), 'ana@correo.com');
    await persona.click(screen.getByRole('button', { name: 'Reenviar el enlace' }));

    // Verificar
    expect(await screen.findByText('Si el correo está registrado, recibirá un nuevo enlace.')).toBeInTheDocument();
    expect(llamadasA(api, 'POST', '/api/cuentas/reenviar-activacion')[0].cuerpo).toEqual({ email: 'ana@correo.com' });
  });

  it.each([['/activar'], ['/activar?token='], ['/activar?token=%20%20']])(
    'sin token (%s) no llama a la API',
    async (ruta) => {
      // Preparar
      const api = simularApi({});

      // Actuar
      abrir(ruta);

      // Verificar
      expect(await screen.findByText('El enlace no trae el código de activación.')).toBeInTheDocument();
      expect(api).not.toHaveBeenCalled();
    },
  );
});

describe('inicio de sesión', () => {
  it('con la cuenta sin activar (403) ofrece reenviar el enlace', async () => {
    // Preparar
    const api = simularApi({
      'POST /api/sesion/iniciar': [403, { error: 'La cuenta no está activa. Revise su correo para activarla.' }],
      'POST /api/cuentas/reenviar-activacion': [200, { mensaje: 'Si el correo está registrado, recibirá un nuevo enlace.' }],
    });
    abrir('/iniciar-sesion');
    const persona = userEvent.setup();

    // Actuar
    await persona.type(await screen.findByLabelText('Correo'), 'ana@correo.com');
    await persona.type(screen.getByLabelText('Contraseña'), 'Habitos2026');
    await persona.click(screen.getByRole('button', { name: 'Iniciar sesión' }));
    await persona.click(await screen.findByRole('button', { name: 'Reenviar el enlace de activación' }));

    // Verificar
    expect(await screen.findByText('Si el correo está registrado, recibirá un nuevo enlace.')).toBeInTheDocument();
    expect(llamadasA(api, 'POST', '/api/cuentas/reenviar-activacion')).toHaveLength(1);
  });

  it('con la cuenta bloqueada (423) muestra el mensaje del servidor sin ofrecer reenvío', async () => {
    // Preparar
    simularApi({ 'POST /api/sesion/iniciar': [423, { error: 'Cuenta bloqueada por 15 minutos.' }] });
    abrir('/iniciar-sesion');
    const persona = userEvent.setup();

    // Actuar
    await persona.type(await screen.findByLabelText('Correo'), 'ana@correo.com');
    await persona.type(screen.getByLabelText('Contraseña'), 'x');
    await persona.click(screen.getByRole('button', { name: 'Iniciar sesión' }));

    // Verificar
    expect(await screen.findByRole('alert')).toHaveTextContent('Cuenta bloqueada por 15 minutos.');
    expect(screen.queryByRole('button', { name: /reenviar/i })).not.toBeInTheDocument();
  });
});

describe('recuperación de contraseña en dos pasos', () => {
  it('pide el código y luego restablece con código y contraseña nueva', async () => {
    // Preparar
    const api = simularApi({
      'POST /api/contrasena/recuperar': [200, { mensaje: 'Si el correo está registrado, recibirá un código.' }],
      'POST /api/contrasena/restablecer': [200, { mensaje: 'Contraseña restablecida. Inicie sesión con la contraseña nueva.' }],
    });
    abrir('/recuperar');
    const persona = userEvent.setup();

    // Actuar: paso 1
    await persona.type(await screen.findByLabelText('Correo'), 'ana@correo.com');
    await persona.click(screen.getByRole('button', { name: 'Enviar código' }));

    // Verificar
    expect(await screen.findByText('Paso 2 de 2')).toBeInTheDocument();

    // Actuar: paso 2
    await persona.type(screen.getByLabelText('Código'), ' K7Q2-M9XP ');
    await persona.type(screen.getByLabelText('Contraseña nueva'), 'Nueva2026x');
    await persona.click(screen.getByRole('button', { name: 'Cambiar contraseña' }));

    // Verificar
    expect(await screen.findByRole('heading', { name: 'Contraseña restablecida' })).toBeInTheDocument();
    expect(llamadasA(api, 'POST', '/api/contrasena/restablecer')[0].cuerpo).toEqual({
      email: 'ana@correo.com',
      codigo: 'K7Q2-M9XP',
      contrasenaNueva: 'Nueva2026x',
    });
  });

  it('un código incorrecto muestra el rechazo y sigue en el paso 2', async () => {
    // Preparar
    simularApi({
      'POST /api/contrasena/recuperar': [200, { mensaje: 'Si el correo está registrado, recibirá un código.' }],
      'POST /api/contrasena/restablecer': [400, { error: 'El código no es válido o venció.' }],
    });
    abrir('/recuperar');
    const persona = userEvent.setup();

    // Actuar
    await persona.type(await screen.findByLabelText('Correo'), 'ana@correo.com');
    await persona.click(screen.getByRole('button', { name: 'Enviar código' }));
    await persona.type(await screen.findByLabelText('Código'), 'ZZZZ-ZZZZ');
    await persona.type(screen.getByLabelText('Contraseña nueva'), 'Nueva2026x');
    await persona.click(screen.getByRole('button', { name: 'Cambiar contraseña' }));

    // Verificar
    expect(await screen.findByRole('alert')).toHaveTextContent('El código no es válido o venció.');
    expect(screen.getByText('Paso 2 de 2')).toBeInTheDocument();
  });
});

describe('cambio de contraseña con sesión', () => {
  it('al cambiarla se cierra la sesión y el inicio muestra el aviso', async () => {
    // Preparar
    conSesionGuardada();
    simularApi({
      '/api/sesion/usuario': [200, usuarioDePrueba()],
      'POST /api/contrasena/cambiar': [200, { mensaje: 'Contraseña actualizada. Inicie sesión de nuevo con la contraseña nueva.' }],
    });
    abrir('/perfil');
    const persona = userEvent.setup();

    // Actuar
    await persona.type(await screen.findByLabelText('Contraseña actual'), 'Habitos2026');
    await persona.type(screen.getByLabelText('Contraseña nueva'), 'Nueva2026x');
    await persona.click(screen.getByRole('button', { name: 'Cambiar contraseña' }));

    // Verificar
    expect(await screen.findByRole('heading', { name: 'Inicia sesión' })).toBeInTheDocument();
    expect(screen.getByText('Contraseña actualizada. Inicie sesión de nuevo con la contraseña nueva.')).toBeInTheDocument();
    expect(localStorage.getItem('rh.sesion')).toBeNull();
  });
});
