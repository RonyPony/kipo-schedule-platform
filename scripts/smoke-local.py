from urllib.request import urlopen

for endpoint in ['live', 'ready']:
    with urlopen(f'http://127.0.0.1:5080/api/v1/health/{endpoint}', timeout=10) as response:
        assert response.status == 200, f'Falló health/{endpoint}.'
        assert response.read().decode('utf-8') == 'Healthy'

print('API viva y conexión con SQL Server local válida: correcto.')
