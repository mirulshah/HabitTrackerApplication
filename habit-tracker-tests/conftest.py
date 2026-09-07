import os
import pytest
import requests
from dotenv import load_dotenv

env_path = os.path.join(os.path.dirname(__file__), ".env")
load_dotenv(env_path)


BASE_URL = os.getenv("BASE_URL")
TEST_EMAIL = os.getenv("TEST_EMAIL")
TEST_PASSWORD = os.getenv("TEST_PASSWORD")

@pytest.fixture(scope="session")
def access_token():
    response = requests.post(
        f"{BASE_URL}/auth/login",
        json={"email": TEST_EMAIL, "password": TEST_PASSWORD},
    )
    response.raise_for_status()
    return response.json()["token"]

@pytest.fixture
def auth_headers(access_token):
    return {"Authorization": f"Bearer { access_token }"}