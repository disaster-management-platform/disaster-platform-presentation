"""Real OIDC regression: repeated sessions over HTTP/2, without logging credentials or cookies."""
import json
import os
from pathlib import Path
import ssl
from urllib.parse import urlsplit

from bs4 import BeautifulSoup
import httpx
import requests
from test_oidc import login

base = os.environ['RAZOR_TEST_URL'].rstrip('/')
assert urlsplit(base).hostname == 'localhost' and urlsplit(base).scheme == 'https'
issuer = os.environ['RAZOR_TEST_ISSUER'].rstrip('/')
assert urlsplit(issuer).hostname == 'localhost' and urlsplit(issuer).scheme == 'https'
identity = json.loads(Path(os.environ['RAZOR_TEST_IDENTITY']).read_text())
session = requests.Session()
session.verify = os.environ['RAZOR_TEST_CA']
http2_tls = ssl.create_default_context(cafile=os.environ['RAZOR_TEST_CA'])

def transient(cookie):
    return cookie.name.startswith(('.AspNetCore.Correlation.', '.AspNetCore.OpenIdConnect.Nonce.'))

# Migrate a previous chunked ticket through Razor, without clearing unrelated browser cookies.
for name, value in [('platform-session', 'chunks-2'), ('platform-sessionC1', 'old-one'), ('platform-sessionC2', 'old-two')]:
    session.cookies.set(name, value, domain='localhost.local', path='/', secure=True)
response = session.get(base + '/Cuenta/Desconectado', timeout=20)
assert response.status_code == 200
assert not any(cookie.name.startswith('platform-session') for cookie in session.cookies)
assert not response.history
print('PASS previous chunked-session migration; signed-out page does not initiate OIDC')

for cycle in range(5):
    session = login(identity, session)
    assert not any(transient(cookie) for cookie in session.cookies)
    tickets = [cookie for cookie in session.cookies if cookie.name.startswith('platform-session')]
    assert len(tickets) == 1 and len(tickets[0].value) < 1024
    response = session.get(base + '/Cuenta/Acceder', timeout=20)
    assert response.status_code == 200 and urlsplit(response.url).path == '/Desastres'
    assert not any(transient(cookie) for cookie in session.cookies)
    assert len(response.history) == 1

    discovery = issuer + '/.well-known/openid-configuration'
    cookie_header = session.prepare_request(requests.Request('GET', discovery)).headers.get('Cookie', '')
    assert len(cookie_header.encode()) < 4096
    with httpx.Client(http2=True, verify=http2_tls, timeout=20) as client:
        response = client.get(discovery, headers={'Cookie': cookie_header})
        assert response.http_version == 'HTTP/2' and response.status_code == 200

    page = session.get(base + '/Desastres', timeout=25)
    form = next(form for form in BeautifulSoup(page.text, 'html.parser').select('form') if 'Cuenta/Salir' in form.get('action', ''))
    csrf = form.select_one('input[name=__RequestVerificationToken]')['value']
    assert session.post(base + '/Cuenta/Salir', timeout=20).status_code == 400
    old_ticket = tickets[0]
    response = session.post(base + '/Cuenta/Salir', data={'__RequestVerificationToken': csrf}, timeout=20)
    assert response.status_code == 200 and urlsplit(response.url).path == '/Cuenta/Desconectado'
    assert not any(cookie.name.startswith('platform-session') or transient(cookie) for cookie in session.cookies)
    assert len(response.history) <= 4
    replay = requests.Session()
    replay.verify = session.verify
    replay.cookies.set(old_ticket.name, old_ticket.value, domain=old_ticket.domain, path=old_ticket.path, secure=True)
    assert replay.get(base + '/Desastres', allow_redirects=False, timeout=20).status_code == 302
    print(f'PASS cycle {cycle + 1}: password login, compact cookie, HTTP/2 Keycloak 200, logout/replay rejection; Cookie bytes={len(cookie_header.encode())}')

# Cancel a real authorization attempt: no error/challenge redirect loop and callback-scoped cookies.
response = session.get(base + '/Cuenta/Acceder', allow_redirects=False, timeout=20)
assert response.status_code == 302
pending = [cookie for cookie in session.cookies if transient(cookie)]
assert len(pending) == 2 and all(cookie.path == '/signin-oidc' and cookie.secure for cookie in pending)
assert all(cookie.name not in session.prepare_request(requests.Request('GET', issuer)).headers.get('Cookie', '') for cookie in pending)
response = session.post(base + '/signin-oidc', data={'error': 'access_denied', 'state': 'invalid-state'}, allow_redirects=False, timeout=20)
assert response.status_code == 400 and 'Location' not in response.headers
print('PASS bounded callback cookies and failed-callback response without redirect loop')
