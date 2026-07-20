"""
Microservice IA (POO Python) - expose la détection via une API REST
consommée par le backend .NET.

Lancer : uvicorn main:app --port 8000 --reload
"""
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Optional
from detection import get_module_detection, modele_reel_disponible
import base64
import io

app = FastAPI(title="SEBN - Microservice de Détection IA")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

detecteur = get_module_detection()


class AnomalieDetectee(BaseModel):
    type_anomalie: str
    classe: str
    confiance: float


class ResultatDetection(BaseModel):
    image_base64: str
    anomalie: Optional[AnomalieDetectee] = None


@app.get("/health")
def health():
    return {"status": "ok", "mode": "reel" if modele_reel_disponible() else "simulation"}


@app.get("/detect", response_model=ResultatDetection)
def detect():
    img, resultat = detecteur.capturer_et_analyser()

    buffer = io.BytesIO()
    img.save(buffer, format="PNG")
    image_b64 = base64.b64encode(buffer.getvalue()).decode()

    anomalie = None
    if resultat:
        anomalie = AnomalieDetectee(
            type_anomalie=resultat["type_anomalie"],
            classe=resultat["classe"],
            confiance=resultat["confiance"],
        )

    return ResultatDetection(image_base64=image_b64, anomalie=anomalie)
