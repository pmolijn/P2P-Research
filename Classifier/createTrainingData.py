from GLOBALS import *
DATALIMIT = 120000

#takes DATALIMIT examples and puts it in necessary format for training
folders1 = os.listdir(AGGREGATESDIR)
csvfiles = []
for eachfolder in folders1:
	folders2 = os.listdir(AGGREGATESDIR + eachfolder)
	for eachname in folders2:
		name = AGGREGATESDIR + eachfolder + '/'+ eachname +'/'
		print name
		if os.path.isdir(name):
			csvfiles += getCSVFiles(name)

outfile = open(TRAININGDIR + 'rawtrainingdata.csv','w')
for filename in csvfiles:
	label2 = filename.split('/')[-2]
	label1 = filename.split('/')[-3]
	inputfile = open(filename)
	count = DATALIMIT - 1
	line = inputfile.readline().strip()
	while count > 0 and line!='':
		fields = line.split(',')
		if float(fields[4])!=0 and float(fields[3])!=0 and float(fields[7])!=0:
			outfile.write(
				fields[2] + ',' +
				fields[3] + ',' +
				fields[4] + ',' +
				fields[7] + ',' +
				label1 + ',' +
				label2 + '\n')
			count -= 1
		line = inputfile.readline().strip()
	inputfile.close()
	print DATALIMIT - count
outfile.close()
