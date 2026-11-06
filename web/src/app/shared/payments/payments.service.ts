import { HttpClient, HttpResponse } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { BaseResponse } from "../models/base-response.interface";

export interface Payment {
    id: number;
    amount: number;
    paidAt: string;
}

export interface PaymentForm {
    amount: number;
    paymentDate: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentsService {
    private _http = inject(HttpClient);

    private url(serviceOrderId: number, paymentId?: number): string {
        const base = `${environment.apiUrl}/service-orders/${serviceOrderId}/payments`;
        return paymentId === undefined ? base : `${base}/${paymentId}`;
    }

    list(serviceOrderId: number): Observable<BaseResponse<Payment[]>> {
        return this._http.get<BaseResponse<Payment[]>>(this.url(serviceOrderId));
    }

    add(serviceOrderId: number, form: PaymentForm): Observable<BaseResponse<string>> {
        return this._http.post<BaseResponse<string>>(this.url(serviceOrderId), form);
    }

    update(serviceOrderId: number, paymentId: number, form: PaymentForm): Observable<BaseResponse<string>> {
        return this._http.put<BaseResponse<string>>(this.url(serviceOrderId, paymentId), form);
    }

    remove(serviceOrderId: number, paymentId: number): Observable<BaseResponse<string>> {
        return this._http.delete<BaseResponse<string>>(this.url(serviceOrderId, paymentId));
    }

    receipt(serviceOrderId: number, paymentId: number): Observable<HttpResponse<Blob>> {
        return this._http.get(`${this.url(serviceOrderId, paymentId)}/receipt`, {
            observe: 'response',
            responseType: 'blob',
        });
    }
}
