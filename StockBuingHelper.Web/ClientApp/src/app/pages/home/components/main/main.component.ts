import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { SbhService } from 'src/app/core/http/sbh.service';
import { LoadingService } from 'src/app/shared/components/loading/loading.service';
import { ReqGetVtiDto } from 'src/app/core/dtos/request/req-get-vti-dto';
import { ResGetVtiDto } from 'src/app/core/dtos/response/res-get-vti-dto';


@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css']
})

export class MainComponent implements OnInit 
{
  vtiRange: number = 800;
  form!:FormGroup;
  vtiRes:ResGetVtiDto[] = [];

  constructor(
    private _formBuilder: FormBuilder,
    private _sbhService: SbhService,
    private _loadingService: LoadingService
  ) { }

  ngOnInit(): void {
    this.form = this._formBuilder.group({
      specificStockId: [''],
      vtiRange: [this.vtiRange],
      etfDisplay : [false],
    })
  }

  submit():void
  {
    this._loadingService.setLoading(true, 'Searching...');

    let data: ReqGetVtiDto = {
      specificStockId: this.form.controls['specificStockId'].value,
      vtiIndex : this.vtiRange,
      queryEtfs: this.form.controls['etfDisplay'].value
    }

    this._sbhService.GetVtiData(data).subscribe({
       next: res => 
       {
        this.vtiRes = res.content;
        this._loadingService.setLoading(false);
        window.alert(res.message);
       },
       error: err =>
       {
        console.log(err);
        this._loadingService.setLoading(false);
       }
    });
  }
}
