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
  //vtiRange: number = 800;
  form!:FormGroup;
  vtiRes:ResGetVtiDto[] = [];
  vtiRange: number[] = [80, 100];
  queryType:string = 'default';

  constructor(
    private _formBuilder: FormBuilder,
    private _sbhService: SbhService,
    private _loadingService: LoadingService
  ) { }

  ngOnInit(): void {
    this.form = this._formBuilder.group({
      specificStockId: [''],
      vtiRanges: [],
      etfDisplay : false
    })
  }

  changeQueryType(event: any)
  {
    this.queryType = event.target.value;

    //恢復default值
    this.vtiRange = [80,100];
    this.form.controls['etfDisplay'].setValue(false);
    this.form.controls['specificStockId'].setValue('');
  }

  submit():void
  {
    this._loadingService.setLoading(true, 'Searching...');

    let data: ReqGetVtiDto = {
      queryType: this.queryType,
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
