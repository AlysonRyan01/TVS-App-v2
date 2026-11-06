import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { BaseResponse } from "../../shared/models/base-response.interface";
import { environment } from "../../../environments/environment";
import { Customer, CustomerForm } from "./models/customer.model";
import { Address } from "./models/address.model";

@Injectable({ providedIn: 'root' })
export class CustomersService {
    private _http = inject(HttpClient);

    searchCustomers(name: string): Observable<BaseResponse<Customer[]>> {
        return this._http.get<BaseResponse<Customer[]>>(`${environment.apiUrl}/customers`, { params: { name } });
    }

    createCustomer(form: CustomerForm): Observable<BaseResponse<Customer>> {
        return this._http.post<BaseResponse<Customer>>(`${environment.apiUrl}/customers`, form);
    }

    updateCustomer(id: number, form: CustomerForm): Observable<BaseResponse<Customer>> {
        return this._http.put<BaseResponse<Customer>>(`${environment.apiUrl}/customers/${id}`, form);
    }

    getAddress(cep: string): Observable<BaseResponse<Address>> {
        return this._http.get<BaseResponse<Address>>(`${environment.apiUrl}/addresses/${cep}`);
    }
}
