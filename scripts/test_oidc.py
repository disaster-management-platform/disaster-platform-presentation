"""Directed OIDC/MFA and real Gateway rendering check with an external synthetic user."""
import base64
import hashlib
import hmac
import json
import os
from pathlib import Path
import struct
import time
from urllib.parse import urljoin, urlsplit

import requests
from bs4 import BeautifulSoup

def login(user=None):
    base = os.environ['RAZOR_TEST_URL'].rstrip('/')
    assert urlsplit(base).scheme == 'https' and urlsplit(base).hostname == 'localhost'
    user = user or json.loads(Path(os.environ['RAZOR_TEST_IDENTITY']).read_text())
    session = requests.Session()
    session.verify = os.environ['RAZOR_TEST_CA']


    def submit(response, values):
        form = BeautifulSoup(response.text, 'html.parser').select_one('form')
        assert form is not None
        payload = {field['name']: field.get('value', '') for field in form.select('input[name]')}
        payload.update(values)
        return session.post(urljoin(response.url, form['action']), data=payload, timeout=20)


    response = session.get(base + '/Cuenta/Acceder', timeout=20)
    response = submit(response, {'username': user['username'], 'password': user['password']})
    assert BeautifulSoup(response.text, 'html.parser').select_one('input[name=otp]') is not None
    secret = user['otpSecret'].replace(' ', '')
    digest = hmac.new(base64.b32decode(secret + '=' * ((-len(secret)) % 8), casefold=True),
                      struct.pack('>Q', int(time.time()) // 30), hashlib.sha256).digest()
    offset = digest[-1] & 15
    code = str((struct.unpack('>I', digest[offset:offset + 4])[0] & 0x7fffffff) % 1000000).zfill(6)
    response = submit(response, {'otp': code})
    if BeautifulSoup(response.text, 'html.parser').select_one('input[name=code]'):
        response = submit(response, {})
    assert response.status_code == 200 and response.url.startswith(base + '/')
    assert any(cookie.name.startswith('platform-session') and cookie.secure for cookie in session.cookies)
    return session


if __name__ == "__main__":
    session = login()
    base = os.environ["RAZOR_TEST_URL"].rstrip("/")
    print('PASS real OIDC callback, MFA and secure session')
    response = session.get(base + '/Organizaciones', timeout=20)
    assert response.status_code == 200 and os.environ['RAZOR_TEST_ORGANIZATION_NAME'] in response.text
    print('PASS authenticated Gateway list envelope rendered by Razor')
    form = next(form for form in BeautifulSoup(response.text, 'html.parser').select('form')
                if 'Cuenta/Salir' in form.get('action', ''))
    csrf = form.select_one('input[name=__RequestVerificationToken]')['value']
    assert session.post(base + '/Cuenta/Salir', timeout=20).status_code == 400
    response = session.post(base + '/Cuenta/Salir', data={'__RequestVerificationToken': csrf}, timeout=20)
    assert response.status_code == 200
    assert not any(cookie.name.startswith('platform-session') for cookie in session.cookies)
    print('PASS antiforgery enforcement and local/issuer logout')
