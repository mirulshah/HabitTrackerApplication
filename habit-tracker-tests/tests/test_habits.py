import os 
import pytest
import requests
from dotenv import load_dotenv

load_dotenv()
BASE_URL = os.getenv("BASE_URL")

print(f"Loaded BASE_URL = {BASE_URL}")

def test_create_habit_with_valid_data_returns_201(auth_headers):
    habit_data = {
        "title": "This is a test habit.",
        "frequency": 1
    }
    response = requests.post(f"{BASE_URL}/habits", json=habit_data, headers=auth_headers)

    assert response.status_code == 200
    body = response.json()
    assert body["title"] == "This is a test habit."
    assert body["frequency"] == 1
    assert body["isCompletedToday"] == False
    assert "createdAt" in body
    assert "updatedAt" in body
    assert "id" in body

def test_create_habit_with_missing_auth_returns_400(auth_headers):
    habit_data = {
        "title": "This is a test habit.",
        "frequency": 1
    }
    response = requests.post(f"{BASE_URL}/habits", json=habit_data)

    assert response.status_code == 401

def test_create_habit_with_missing_title_returns_400(auth_headers):
    habit_data = {
        "frequency": 1
    }
    response = requests.post(f"{BASE_URL}/habits", json=habit_data, headers=auth_headers)

    assert response.status_code == 400

def test_create_habit_with_invalid_data_returns_400(auth_headers):
    habit_data = {
        "title": "",
        "frequency": 1
    }
    response = requests.post(f"{BASE_URL}/habits", json=habit_data, headers=auth_headers)

    assert response.status_code == 400

